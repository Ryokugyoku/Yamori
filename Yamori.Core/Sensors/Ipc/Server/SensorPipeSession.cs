using System.IO.Pipes;
using System.Threading.Channels;
using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Sensors.Ipc.Server;

/// <summary>
/// クライアント 1 つとの接続。版と受け取りたい通知の種類を確かめてから、依頼に返事をし、選ばれた種類の通知だけを送る。
/// </summary>
internal sealed class SensorPipeSession
{
    private const int MaxConcurrentRequests = 4;

    private readonly NamedPipeServerStream _pipe;
    private readonly SensorRequestHandler _handler;
    private readonly SensorPipeServerOptions _options;
    private readonly Channel<IpcEnvelope> _notifications;
    private readonly SemaphoreSlim _requestSlots = new(MaxConcurrentRequests, MaxConcurrentRequests);
    private volatile bool _accepted;
    private volatile int _topics;

    /// <summary>
    /// 接続済みのパイプと、依頼の処理を指定して作る。
    /// </summary>
    /// <param name="pipe">接続済みのパイプ。接続が終わったら閉じる。</param>
    /// <param name="handler">依頼の処理。</param>
    /// <param name="options">サーバーの設定。</param>
    public SensorPipeSession(NamedPipeServerStream pipe, SensorRequestHandler handler, SensorPipeServerOptions options)
    {
        _pipe = pipe;
        _handler = handler;
        _options = options;
        _notifications = Channel.CreateBounded<IpcEnvelope>(new BoundedChannelOptions(options.NotificationQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });
    }

    /// <summary>
    /// この接続が、指定した種類の通知を受け取りたいかどうかを調べる（版を確かめ終わる前は false）。
    /// </summary>
    /// <param name="kind">通知の種類。</param>
    /// <returns>受け取りたいなら true。</returns>
    public bool Wants(SensorNotificationKind kind) => _accepted && ((SensorNotificationTopics)_topics).Includes(kind);

    /// <summary>
    /// 通知を送る順番に並べる。版を確かめ終わる前の通知と、相手が選ばなかった種類の通知は捨てる。遅いクライアントの通知は古いものから捨てる。
    /// </summary>
    /// <param name="notification">通知。</param>
    public void Enqueue(IpcEnvelope notification)
    {
        if (Wants(notification.Notification))
        {
            _notifications.Writer.TryWrite(notification);
        }
    }

    /// <summary>
    /// 接続が切れるか、止める合図が来るまでやり取りを続ける。
    /// </summary>
    /// <param name="cancellationToken">止める合図。</param>
    /// <returns>接続が終わったときに完了するタスク。例外は投げない。</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var connection = new IpcConnection(_pipe);
        using var sessionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task pump = Task.CompletedTask;
        try
        {
            var isAdministrator = await HandshakeAsync(connection, cancellationToken).ConfigureAwait(false);
            if (isAdministrator is null)
            {
                return;
            }

            _accepted = true;
            pump = PumpNotificationsAsync(connection, sessionCancellation.Token);
            await ReceiveRequestsAsync(connection, isAdministrator.Value, sessionCancellation.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or ObjectDisposedException or OperationCanceledException)
        {
            // 接続が切れた、相手が壊れたデータを送った、止める合図が来た。この接続だけを終える。
        }
        finally
        {
            _accepted = false;
            _notifications.Writer.TryComplete();
            await sessionCancellation.CancelAsync().ConfigureAwait(false);
            await pump.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            for (var i = 0; i < MaxConcurrentRequests; i++)
            {
                // 処理中の依頼が終わるのを待ってから、パイプを閉じる。
                await _requestSlots.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    /// <returns>受け入れたら、相手が管理者かどうか。受け入れなかったら null。</returns>
    private async Task<bool?> HandshakeAsync(IpcConnection connection, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.HandshakeTimeout);

        var hello = await connection.ReceiveAsync(timeout.Token).ConfigureAwait(false);
        if (hello is null || hello.Kind != IpcMessageKind.Hello)
        {
            return null;
        }

        var accepted = SensorContract.IsCompatible(hello.Version);
        _topics = (int)hello.Topics;
        await connection.SendAsync(
            new IpcEnvelope { Kind = IpcMessageKind.HelloReply, Version = SensorContract.Version, Accepted = accepted },
            timeout.Token).ConfigureAwait(false);

        // 相手から読んだ後でなければ、相手の権限は調べられない。
        return accepted ? _options.IsAdministrator(_pipe) : null;
    }

    private async Task ReceiveRequestsAsync(IpcConnection connection, bool isAdministrator, CancellationToken cancellationToken)
    {
        while (await connection.ReceiveAsync(cancellationToken).ConfigureAwait(false) is { } message)
        {
            if (message.Kind != IpcMessageKind.Request)
            {
                // 知らない種類のメッセージは無視する（新しい版のクライアント）。
                continue;
            }

            await _requestSlots.WaitAsync(cancellationToken).ConfigureAwait(false);
            _ = HandleOneAsync(connection, message, isAdministrator, cancellationToken);
        }
    }

    private async Task HandleOneAsync(IpcConnection connection, IpcEnvelope request, bool isAdministrator, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _handler.HandleAsync(request, isAdministrator, cancellationToken).ConfigureAwait(false);
            await connection.SendAsync(response, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
        {
            // 返事を送る前に接続が切れた。
        }
        finally
        {
            _requestSlots.Release();
        }
    }

    private async Task PumpNotificationsAsync(IpcConnection connection, CancellationToken cancellationToken)
    {
        await foreach (var notification in _notifications.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            await connection.SendAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}

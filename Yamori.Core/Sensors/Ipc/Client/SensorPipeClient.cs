using System.Collections.Concurrent;
using System.Text.Json;
using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Sensors.Ipc.Client;

/// <summary>
/// センサーのパイプのクライアント 1 本分。版を確かめてつなぎ、依頼と返事を対応させ、通知を受け取る。
/// 切れたらつなぎ直さない（つなぎ直しは <see cref="RemoteSensorService"/> が行う）。
/// </summary>
public sealed class SensorPipeClient : IAsyncDisposable
{
    private readonly IpcConnection _connection;
    private readonly ConcurrentDictionary<long, TaskCompletionSource<IpcEnvelope>> _pending = new();
    private readonly CancellationTokenSource _closing = new();
    private long _nextId;
    private volatile bool _closed;
    private Task _receiveLoop = Task.CompletedTask;

    private SensorPipeClient(IpcConnection connection, int serverVersion)
    {
        _connection = connection;
        ServerVersion = serverVersion;
    }

    /// <summary>
    /// サーバーから通知が届いたときに発生する。受け取りのスレッドから発生する。
    /// </summary>
    public event EventHandler<IpcEnvelope>? NotificationReceived;

    /// <summary>サーバーの版を取得する。</summary>
    public int ServerVersion { get; }

    /// <summary>接続が切れたときに完了するタスクを取得する。</summary>
    public Task Completion => _receiveLoop;

    /// <summary>
    /// つないで版を確かめる。受け取りは <see cref="Start"/> を呼んでから始まる。
    /// </summary>
    /// <param name="stream">接続済みのパイプ。失敗した場合は閉じる。</param>
    /// <param name="handshakeTimeout">版の返事を待つ最長の時間。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>つないだクライアント。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> が null。</exception>
    /// <exception cref="SensorContractMismatchException">サーバーの版とやり取りできない。</exception>
    /// <exception cref="IOException">返事が来る前に接続が切れた。</exception>
    /// <exception cref="TimeoutException">返事が来なかった。</exception>
    public static async Task<SensorPipeClient> ConnectAsync(Stream stream, TimeSpan handshakeTimeout, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var connection = new IpcConnection(stream);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(handshakeTimeout);

            IpcEnvelope? reply;
            try
            {
                await connection.SendAsync(new IpcEnvelope { Kind = IpcMessageKind.Hello, Version = SensorContract.Version }, timeout.Token).ConfigureAwait(false);
                reply = await connection.ReceiveAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("Helper から版の返事が来ませんでした。");
            }
            catch (InvalidDataException ex)
            {
                throw new IOException("Helper の返事を読めません。", ex);
            }

            if (reply is null || reply.Kind != IpcMessageKind.HelloReply)
            {
                throw new IOException("Helper が版の返事を送る前に接続を閉じました。");
            }

            if (!reply.Accepted || !SensorContract.IsCompatible(reply.Version))
            {
                throw new SensorContractMismatchException(reply.Version);
            }

            return new SensorPipeClient(connection, reply.Version);
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// 受け取りを始める。<see cref="NotificationReceived"/> を購読してから呼ぶこと。
    /// </summary>
    public void Start() => _receiveLoop = Task.Run(ReceiveLoopAsync);

    /// <summary>
    /// 依頼を送り、返事を待つ。
    /// </summary>
    /// <param name="operation">操作。</param>
    /// <param name="payload">引数。ない場合は null。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>返事。</returns>
    /// <exception cref="IOException">接続が切れた。</exception>
    /// <exception cref="OperationCanceledException">中止された。</exception>
    public async Task<IpcEnvelope> RequestAsync(SensorOperation operation, JsonElement? payload, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _nextId);
        var completion = new TaskCompletionSource<IpcEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = completion;
        try
        {
            if (_closed)
            {
                throw new IOException("Helper との接続が切れています。");
            }

            try
            {
                await _connection.SendAsync(new IpcEnvelope { Kind = IpcMessageKind.Request, Id = id, Operation = operation, Payload = payload }, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ObjectDisposedException ex)
            {
                throw new IOException("Helper との接続が切れています。", ex);
            }

            return await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _pending.TryRemove(id, out _);
        }
    }

    /// <summary>
    /// 接続を閉じ、返事を待っている依頼を失敗させる。
    /// </summary>
    /// <returns>閉じたときに完了するタスク。</returns>
    public async ValueTask DisposeAsync()
    {
        await _closing.CancelAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
        await _receiveLoop.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        FailPending();
        _closing.Dispose();
    }

    private async Task ReceiveLoopAsync()
    {
        try
        {
            while (await _connection.ReceiveAsync(_closing.Token).ConfigureAwait(false) is { } message)
            {
                switch (message.Kind)
                {
                    case IpcMessageKind.Response when _pending.TryGetValue(message.Id, out var completion):
                        completion.TrySetResult(message);
                        break;
                    case IpcMessageKind.Notification:
                        NotificationReceived?.Invoke(this, message);
                        break;
                    default:
                        // 待っていない返事や、知らない種類のメッセージは無視する。
                        break;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or ObjectDisposedException or OperationCanceledException)
        {
            // 接続が切れた。
        }
        finally
        {
            FailPending();
        }
    }

    private void FailPending()
    {
        _closed = true;
        foreach (var completion in _pending.Values)
        {
            completion.TrySetException(new IOException("Helper との接続が切れました。"));
        }
    }
}

using Yamori.Core.Anomalies;
using Yamori.Core.Notifications;
using Yamori.Core.Sensors.Ipc;
using Yamori.Core.Sensors.Ipc.Client;

namespace Yamori.Notifier.Helper;

/// <summary>
/// Helper のパイプに「異常の知らせだけ」を受け取る形でつなぎ、届いた知らせを <see cref="AnomalyNotificationDispatcher"/> に渡す。
/// パイプは UI スレッドとは別のスレッド（スレッド プール）で動かす。切れたら少し待ってつなぎ直し、Helper が動いていない間も待ち続ける。
/// </summary>
internal sealed class HelperAnomalyListener : IDisposable
{
    /// <summary>Helper のパイプにつながるまで待つ最長の時間。</summary>
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(1);

    /// <summary>つながらなかった・切れたときに、つなぎ直すまでの待ち時間。サインイン直後は Helper の起動を待つことが多いので、画面より長めにする。</summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    /// <summary>止めるときに、パイプの処理が終わるのを待つ最長の時間。</summary>
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(3);

    private readonly RemoteSensorService _remote;
    private readonly Func<RemoteSensorService, AnomalyNotificationDispatcher> _createDispatcher;
    private readonly CancellationTokenSource _stop = new();
    private readonly CancellationToken _token;
    private AnomalyNotificationDispatcher? _dispatcher;
    private Task? _run;

    /// <summary>
    /// 知らせを通知にする部品の作り方を指定して作る。まだつながない（<see cref="Start"/> で始める）。
    /// </summary>
    /// <param name="createDispatcher">パイプの窓口から、知らせを通知にする部品を作る処理。</param>
    /// <exception cref="ArgumentNullException"><paramref name="createDispatcher"/> が null。</exception>
    public HelperAnomalyListener(Func<RemoteSensorService, AnomalyNotificationDispatcher> createDispatcher)
    {
        ArgumentNullException.ThrowIfNull(createDispatcher);
        _createDispatcher = createDispatcher;
        _token = _stop.Token;
        _remote = new RemoteSensorService(
            SensorPipeConnector.Create(SensorPipeNames.Sensors, ConnectTimeout),
            TimeProvider.System,
            new RemoteSensorServiceOptions { Topics = SensorNotificationTopics.Anomalies, RetryDelay = RetryDelay });
    }

    /// <summary>
    /// Helper とのつながりの状態が変わったときに発生する。パイプのスレッドから発生するので、画面の更新は UI スレッドへ渡すこと。
    /// </summary>
    public event EventHandler<HelperConnectionState>? ConnectionStateChanged;

    /// <summary>
    /// パイプにつなぎ始める。戻るのを待たない。
    /// </summary>
    /// <exception cref="InvalidOperationException">すでに始めている。</exception>
    public void Start()
    {
        if (_run is not null)
        {
            throw new InvalidOperationException("すでに始めています。");
        }

        _dispatcher = _createDispatcher(_remote);
        _remote.AnomalyChanged += OnAnomalyChanged;
        _remote.ConnectionStateChanged += OnConnectionStateChanged;
        _run = Task.Run(() => _remote.RunAsync(_token));
    }

    /// <summary>
    /// パイプを閉じ、処理が終わるのを少しだけ待つ。
    /// </summary>
    public void Dispose()
    {
        _remote.AnomalyChanged -= OnAnomalyChanged;
        _remote.ConnectionStateChanged -= OnConnectionStateChanged;
        _stop.Cancel();
        try
        {
            _run?.Wait(StopTimeout);
        }
        catch (AggregateException ex)
        {
            NotifierLog.Write("HelperAnomalyListener.Dispose", ex);
        }

        _stop.Dispose();
    }

    private void OnConnectionStateChanged(object? sender, HelperConnectionState state) =>
        ConnectionStateChanged?.Invoke(this, state);

    private void OnAnomalyChanged(object? sender, AnomalyNotice notice)
    {
        if (_dispatcher is not { } dispatcher || !AnomalyToastFilter.IsCandidate(notice))
        {
            return;
        }

        // パイプの読み取りを止めないよう、待たずに処理する（知らせはまれなので、順番は dispatcher がそろえる）。
        _ = HandleAsync(dispatcher, notice);
    }

    private async Task HandleAsync(AnomalyNotificationDispatcher dispatcher, AnomalyNotice notice)
    {
        try
        {
            await dispatcher.HandleAsync(notice, _token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_token.IsCancellationRequested)
        {
            // 終了中。
        }
        catch (Exception ex)
        {
            NotifierLog.Write("AnomalyNotification", ex);
        }
    }
}

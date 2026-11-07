using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Sensors.Ipc.Client;

/// <summary>
/// GUI で使う <see cref="ISensorService"/>。Helper につながっている間は Helper（<see cref="RemoteSensorService"/>）を使い、
/// つながっていない間は、GUI の中で標準 API だけを読む代わりの表示（記録なし）に切り替える。
/// </summary>
/// <remarks>
/// 通知は、そのときに使っている側からのものだけを流す。切り替えたときは、新しい側のカタログと拡張センサーの状態を通知する。
/// </remarks>
public sealed class FailoverSensorService : ISensorService, IHelperConnection, IDisposable
{
    private readonly RemoteSensorService _remote;
    private readonly Func<LocalSensorService> _createFallback;
    private readonly Lock _gate = new();
    private LocalSensorService? _fallback;
    private CancellationTokenSource? _fallbackCancellation;
    private Task _fallbackRun = Task.CompletedTask;
    private bool _stopped;

    /// <summary>
    /// Helper への接続と、代わりの表示の作り方を指定して作る。
    /// </summary>
    /// <param name="remote">Helper への接続。</param>
    /// <param name="createFallback">代わりの表示を作る処理（<see cref="LocalSensorServiceFactory.CreateDisplayOnly"/>）。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public FailoverSensorService(RemoteSensorService remote, Func<LocalSensorService> createFallback)
    {
        ArgumentNullException.ThrowIfNull(remote);
        ArgumentNullException.ThrowIfNull(createFallback);
        _remote = remote;
        _createFallback = createFallback;

        _remote.ReadingsUpdated += OnRemoteReadings;
        _remote.CatalogChanged += OnRemoteCatalog;
        _remote.ExtendedSensorStateChanged += OnRemoteExtendedState;
        _remote.ConnectionStateChanged += OnConnectionStateChanged;
    }

    /// <inheritdoc />
    public event EventHandler<IReadOnlyList<SensorReading>>? ReadingsUpdated;

    /// <inheritdoc />
    public event EventHandler<IReadOnlyList<SensorDescriptor>>? CatalogChanged;

    /// <inheritdoc />
    public event EventHandler<ExtendedSensorState>? ExtendedSensorStateChanged;

    /// <inheritdoc />
    public event EventHandler<HelperConnectionState>? ConnectionStateChanged;

    /// <inheritdoc />
    public int ContractVersion => SensorContract.Version;

    /// <inheritdoc />
    public HelperConnectionState ConnectionState => _remote.ConnectionState;

    /// <summary>代わりの表示（GUI の中での標準 API の読み取り）を使っているかどうかを取得する。</summary>
    public bool IsUsingFallback
    {
        get
        {
            lock (_gate)
            {
                return _fallback is not null;
            }
        }
    }

    private ISensorService Active
    {
        get
        {
            lock (_gate)
            {
                return (ISensorService?)_fallback ?? _remote;
            }
        }
    }

    /// <summary>
    /// 止める合図が来るまで、Helper への接続を続ける（つながらない間は代わりの表示を動かす）。
    /// </summary>
    /// <param name="cancellationToken">止める合図。</param>
    /// <returns>止まったときに完了するタスク。止める合図による停止では例外を投げない。</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _remote.RunAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Task run;
            lock (_gate)
            {
                _stopped = true;
                run = StopFallbackLocked();
            }

            await run.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SensorDescriptor>> GetCatalogAsync(CancellationToken cancellationToken = default) =>
        Active.GetCatalogAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<SensorReading>> GetLatestAsync(IReadOnlyCollection<SensorKey> keys, CancellationToken cancellationToken = default) =>
        Active.GetLatestAsync(keys, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<SensorReading>> GetRecentAsync(SensorKey key, TimeSpan window, CancellationToken cancellationToken = default) =>
        Active.GetRecentAsync(key, window, cancellationToken);

    /// <inheritdoc />
    public Task<ExtendedSensorState> GetExtendedSensorStateAsync(CancellationToken cancellationToken = default) =>
        Active.GetExtendedSensorStateAsync(cancellationToken);

    /// <inheritdoc />
    /// <remarks>履歴は Helper だけが持つ。代わりの表示の間は空。</remarks>
    public Task<IReadOnlyList<SensorHistoryPoint>> GetHistoryAsync(
        SensorKey key,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        TimeSeriesResolution resolution,
        CancellationToken cancellationToken = default) =>
        _remote.GetHistoryAsync(key, fromUtc, toUtc, resolution, cancellationToken);

    /// <inheritdoc />
    public Task<SensorLogPolicy?> GetLogPolicyAsync(SensorKey key, CancellationToken cancellationToken = default) =>
        _remote.GetLogPolicyAsync(key, cancellationToken);

    /// <inheritdoc />
    public Task<SensorSettingResult> SetLogPolicyAsync(SensorKey key, SensorLogPolicy policy, CancellationToken cancellationToken = default) =>
        _remote.SetLogPolicyAsync(key, policy, cancellationToken);

    /// <inheritdoc />
    public Task<bool> GetPawnIoConsentAsync(CancellationToken cancellationToken = default) =>
        _remote.GetPawnIoConsentAsync(cancellationToken);

    /// <inheritdoc />
    public Task<SensorSettingResult> SetPawnIoConsentAsync(bool enabled, CancellationToken cancellationToken = default) =>
        _remote.SetPawnIoConsentAsync(enabled, cancellationToken);

    /// <summary>
    /// 購読を解除する。代わりの表示は <see cref="RunAsync"/> の終わりで止めて破棄する。
    /// </summary>
    public void Dispose()
    {
        _remote.ReadingsUpdated -= OnRemoteReadings;
        _remote.CatalogChanged -= OnRemoteCatalog;
        _remote.ExtendedSensorStateChanged -= OnRemoteExtendedState;
        _remote.ConnectionStateChanged -= OnConnectionStateChanged;
    }

    private void OnConnectionStateChanged(object? sender, HelperConnectionState state)
    {
        lock (_gate)
        {
            if (state == HelperConnectionState.Connected)
            {
                _ = StopFallbackLocked();
            }
            else if (!_stopped && _fallback is null)
            {
                StartFallbackLocked();
            }
        }

        ConnectionStateChanged?.Invoke(this, state);
    }

    private void StartFallbackLocked()
    {
        var fallback = _createFallback();
        var cancellation = new CancellationTokenSource();
        fallback.ReadingsUpdated += OnFallbackReadings;
        fallback.CatalogChanged += OnFallbackCatalog;
        _fallback = fallback;
        _fallbackCancellation = cancellation;
        _fallbackRun = Task.Run(() => fallback.RunAsync(cancellation.Token));

        // 代わりの表示の拡張センサーの状態（Helper が必要、または PawnIO が入っていない）を伝える。
        _ = fallback.GetExtendedSensorStateAsync().ContinueWith(
            t => ExtendedSensorStateChanged?.Invoke(this, t.Result),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.Default);
    }

    private Task StopFallbackLocked()
    {
        if (_fallback is not { } fallback || _fallbackCancellation is not { } cancellation)
        {
            return _fallbackRun;
        }

        fallback.ReadingsUpdated -= OnFallbackReadings;
        fallback.CatalogChanged -= OnFallbackCatalog;
        _fallback = null;
        _fallbackCancellation = null;
        cancellation.Cancel();

        // 収集が止まってから破棄する。
        _fallbackRun = _fallbackRun.ContinueWith(
            _ =>
            {
                fallback.Dispose();
                cancellation.Dispose();
            },
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);
        return _fallbackRun;
    }

    private bool IsCurrentFallback(object? sender)
    {
        lock (_gate)
        {
            return sender is not null && ReferenceEquals(sender, _fallback);
        }
    }

    private bool IsRemoteActive()
    {
        lock (_gate)
        {
            return _fallback is null;
        }
    }

    private void OnRemoteReadings(object? sender, IReadOnlyList<SensorReading> readings)
    {
        if (IsRemoteActive())
        {
            ReadingsUpdated?.Invoke(this, readings);
        }
    }

    private void OnRemoteCatalog(object? sender, IReadOnlyList<SensorDescriptor> catalog)
    {
        if (IsRemoteActive())
        {
            CatalogChanged?.Invoke(this, catalog);
        }
    }

    private void OnRemoteExtendedState(object? sender, ExtendedSensorState state)
    {
        if (IsRemoteActive())
        {
            ExtendedSensorStateChanged?.Invoke(this, state);
        }
    }

    private void OnFallbackReadings(object? sender, IReadOnlyList<SensorReading> readings)
    {
        if (IsCurrentFallback(sender))
        {
            ReadingsUpdated?.Invoke(this, readings);
        }
    }

    private void OnFallbackCatalog(object? sender, IReadOnlyList<SensorDescriptor> catalog)
    {
        if (IsCurrentFallback(sender))
        {
            CatalogChanged?.Invoke(this, catalog);
        }
    }
}

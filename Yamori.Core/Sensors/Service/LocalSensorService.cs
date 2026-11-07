using Yamori.Core.Sensors.Collection;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Hot;
using Yamori.Core.Sensors.Persistence;
using Yamori.Core.Sensors.Providers;
using Yamori.Core.Sensors.Registry;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Sensors.Service;

/// <summary>
/// 収集とストアを同じプロセスの中で動かす <see cref="ISensorService"/>。
/// Helper のプロセスでは記録ありで使い、GUI のプロセスでは Helper が動いていない間の代わりの表示として、記録なしで使う。
/// </summary>
/// <remarks>
/// 収集と永続化は <see cref="RunAsync"/> を呼んでいる間だけ行う。永続化（時系列・マスタ）がない場合は、
/// 最新値と直近の履歴（Hot ストア）だけで動く。
/// </remarks>
public sealed class LocalSensorService : ISensorService, IDisposable
{
    private readonly SensorRegistry _registry;
    private readonly IHotStore _hotStore;
    private readonly SensorCollectionLoop _loop;
    private readonly SensorPersistence? _persistence;
    private readonly IExtendedSensorControl _extended;
    private readonly TimeProvider _timeProvider;
    private readonly IReadOnlyList<IDisposable> _ownedResources;
    private readonly Lock _stateLock = new();
    private ExtendedSensorState _lastExtendedState;

    /// <summary>
    /// 部品を指定して作る。
    /// </summary>
    /// <param name="registry">センサーの統合。</param>
    /// <param name="hotStore">最新値と直近の履歴のストア。</param>
    /// <param name="loop">収集の繰り返し。</param>
    /// <param name="persistence">永続化。記録しない場合は null。</param>
    /// <param name="extended">拡張センサーの判断と同意の扱い。</param>
    /// <param name="timeProvider">現在時刻の提供元。</param>
    /// <param name="ownedResources">破棄するときに一緒に破棄するもの（Provider、ストア、通知の登録）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="persistence"/> 以外の引数が null。</exception>
    public LocalSensorService(
        SensorRegistry registry,
        IHotStore hotStore,
        SensorCollectionLoop loop,
        SensorPersistence? persistence,
        IExtendedSensorControl extended,
        TimeProvider timeProvider,
        IReadOnlyList<IDisposable> ownedResources)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(hotStore);
        ArgumentNullException.ThrowIfNull(loop);
        ArgumentNullException.ThrowIfNull(extended);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(ownedResources);

        _registry = registry;
        _hotStore = hotStore;
        _loop = loop;
        _persistence = persistence;
        _extended = extended;
        _timeProvider = timeProvider;
        _ownedResources = ownedResources;
        _lastExtendedState = CurrentExtendedState();

        _hotStore.Appended += OnAppended;
        _registry.CatalogChanged += OnCatalogChanged;
        _extended.ConsentChanged += OnConsentChanged;
    }

    /// <inheritdoc />
    public event EventHandler<IReadOnlyList<SensorReading>>? ReadingsUpdated;

    /// <inheritdoc />
    public event EventHandler<IReadOnlyList<SensorDescriptor>>? CatalogChanged;

    /// <inheritdoc />
    public event EventHandler<ExtendedSensorState>? ExtendedSensorStateChanged;

    /// <inheritdoc />
    public int ContractVersion => SensorContract.Version;

    /// <summary>時系列とマスタへの記録が使えるかどうかを取得する。</summary>
    public bool IsPersistenceAvailable => _persistence is not null;

    /// <summary>
    /// キャンセルされるまで収集と永続化を続ける。
    /// </summary>
    /// <param name="cancellationToken">停止の合図。</param>
    /// <returns>停止したときに完了するタスク。キャンセルによる停止では例外を投げない。</returns>
    public Task RunAsync(CancellationToken cancellationToken) =>
        _persistence is null
            ? _loop.RunAsync(cancellationToken)
            : Task.WhenAll(_loop.RunAsync(cancellationToken), _persistence.RunAsync(cancellationToken));

    /// <inheritdoc />
    public Task<IReadOnlyList<SensorDescriptor>> GetCatalogAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_registry.Catalog);

    /// <inheritdoc />
    public Task<IReadOnlyList<SensorReading>> GetLatestAsync(IReadOnlyCollection<SensorKey> keys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var now = _timeProvider.GetUtcNow();
        IReadOnlyList<SensorReading> result = keys
            .Select(k => _hotStore.GetLatest(k)
                ?? SensorReading.Failed(
                    k,
                    _registry.TryGetDescriptor(k, out _) ? SensorStatus.TemporarilyUnavailable : SensorStatus.Unsupported,
                    now,
                    string.Empty))
            .ToArray();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SensorReading>> GetRecentAsync(SensorKey key, TimeSpan window, CancellationToken cancellationToken = default) =>
        Task.FromResult(_hotStore.GetRecent(key, window));

    /// <inheritdoc />
    public Task<ExtendedSensorState> GetExtendedSensorStateAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CurrentExtendedState());

    /// <inheritdoc />
    public Task<IReadOnlyList<SensorHistoryPoint>> GetHistoryAsync(
        SensorKey key,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        TimeSeriesResolution resolution,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => _persistence?.GetHistory(key, fromUtc, toUtc, resolution) ?? [], cancellationToken);

    /// <inheritdoc />
    public Task<SensorLogPolicy?> GetLogPolicyAsync(SensorKey key, CancellationToken cancellationToken = default) =>
        Task.Run(() => _persistence?.GetLogPolicy(key), cancellationToken);

    /// <inheritdoc />
    public Task<SensorSettingResult> SetLogPolicyAsync(SensorKey key, SensorLogPolicy policy, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return Task.Run(
            () => _persistence is null ? SensorSettingResult.Unavailable
                : _persistence.SetLogPolicy(key, policy) ? SensorSettingResult.Applied
                : SensorSettingResult.NotFound,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> GetPawnIoConsentAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_extended.IsConsented);

    /// <inheritdoc />
    public Task<SensorSettingResult> SetPawnIoConsentAsync(bool enabled, CancellationToken cancellationToken = default) =>
        Task.Run(() => _extended.SetConsent(enabled), cancellationToken);

    /// <summary>
    /// イベントの購読を解除し、持っている Provider やストアなどを破棄する。<see cref="RunAsync"/> を止めてから呼ぶこと。
    /// </summary>
    public void Dispose()
    {
        _hotStore.Appended -= OnAppended;
        _registry.CatalogChanged -= OnCatalogChanged;
        _extended.ConsentChanged -= OnConsentChanged;
        _loop.Dispose();
        foreach (var resource in _ownedResources)
        {
            resource.Dispose();
        }
    }

    private ExtendedSensorState CurrentExtendedState() =>
        _extended.GetState(_registry.FailedProviderIds.Contains(SensorProviderIds.PawnIo));

    private void OnAppended(object? sender, IReadOnlyList<SensorReading> readings)
    {
        _persistence?.OnReadings(readings);
        ReadingsUpdated?.Invoke(this, readings);
    }

    private void OnCatalogChanged(object? sender, IReadOnlyList<SensorDescriptor> catalog)
    {
        try
        {
            _persistence?.OnCatalogChanged(catalog);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // マスタに書けなくても、収集と表示は止めない（記録の対象は前のまま）。
        }

        CatalogChanged?.Invoke(this, catalog);
        RaiseExtendedStateIfChanged();
    }

    private void OnConsentChanged(object? sender, EventArgs e) => RaiseExtendedStateIfChanged();

    private void RaiseExtendedStateIfChanged()
    {
        var state = CurrentExtendedState();
        lock (_stateLock)
        {
            if (state == _lastExtendedState)
            {
                return;
            }

            _lastExtendedState = state;
        }

        ExtendedSensorStateChanged?.Invoke(this, state);
    }
}

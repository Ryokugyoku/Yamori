using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// 返す値と通知をテストから決められる <see cref="ISensorService"/>（パイプのサーバーの相手）。
/// </summary>
internal sealed class FakeSensorService : ISensorService
{
    public event EventHandler<IReadOnlyList<SensorReading>>? ReadingsUpdated;

    public event EventHandler<IReadOnlyList<SensorDescriptor>>? CatalogChanged;

    public event EventHandler<ExtendedSensorState>? ExtendedSensorStateChanged;

    public int ContractVersion => SensorContract.Version;

    public List<SensorDescriptor> Catalog { get; } = [];

    public Dictionary<SensorKey, SensorReading> Latest { get; } = [];

    public List<SensorHistoryPoint> History { get; } = [];

    public Dictionary<SensorKey, SensorLogPolicy> Policies { get; } = [];

    public ExtendedSensorState ExtendedState { get; set; } = ExtendedSensorState.ConsentRequired;

    public bool Consent { get; set; }

    public int SetCalls { get; private set; }

    public void RaiseReadings(IReadOnlyList<SensorReading> readings) => ReadingsUpdated?.Invoke(this, readings);

    public void RaiseCatalog() => CatalogChanged?.Invoke(this, Catalog.ToArray());

    public void RaiseExtendedState(ExtendedSensorState state)
    {
        ExtendedState = state;
        ExtendedSensorStateChanged?.Invoke(this, state);
    }

    public Task<IReadOnlyList<SensorDescriptor>> GetCatalogAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SensorDescriptor>>(Catalog.ToArray());

    public Task<IReadOnlyList<SensorReading>> GetLatestAsync(IReadOnlyCollection<SensorKey> keys, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SensorReading>>(keys
            .Select(k => Latest.GetValueOrDefault(k) ?? SensorReading.Failed(k, SensorStatus.Unsupported, DateTimeOffset.UnixEpoch, string.Empty))
            .ToArray());

    public Task<IReadOnlyList<SensorReading>> GetRecentAsync(SensorKey key, TimeSpan window, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SensorReading>>(Latest.TryGetValue(key, out var r) ? [r] : []);

    public Task<ExtendedSensorState> GetExtendedSensorStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(ExtendedState);

    public List<(SensorKey Key, DateTimeOffset From, DateTimeOffset To, TimeSeriesResolution Resolution)> HistoryRequests { get; } = [];

    public Task<IReadOnlyList<SensorHistoryPoint>> GetHistoryAsync(
        SensorKey key, DateTimeOffset fromUtc, DateTimeOffset toUtc, TimeSeriesResolution resolution, CancellationToken cancellationToken = default)
    {
        HistoryRequests.Add((key, fromUtc, toUtc, resolution));
        return Task.FromResult<IReadOnlyList<SensorHistoryPoint>>(History.Where(p => p.TimestampUtc >= fromUtc && p.TimestampUtc < toUtc).ToArray());
    }

    public Task<SensorLogPolicy?> GetLogPolicyAsync(SensorKey key, CancellationToken cancellationToken = default) =>
        Task.FromResult(Policies.GetValueOrDefault(key));

    public Task<SensorSettingResult> SetLogPolicyAsync(SensorKey key, SensorLogPolicy policy, CancellationToken cancellationToken = default)
    {
        SetCalls++;
        if (!Policies.ContainsKey(key))
        {
            return Task.FromResult(SensorSettingResult.NotFound);
        }

        Policies[key] = policy;
        return Task.FromResult(SensorSettingResult.Applied);
    }

    public Task<bool> GetPawnIoConsentAsync(CancellationToken cancellationToken = default) => Task.FromResult(Consent);

    public Task<SensorSettingResult> SetPawnIoConsentAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        SetCalls++;
        Consent = enabled;
        return Task.FromResult(SensorSettingResult.Applied);
    }
}

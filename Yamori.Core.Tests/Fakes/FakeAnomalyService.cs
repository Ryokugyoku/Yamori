using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Service;
using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// 決まった異常と設定を返し、受けた依頼を覚えておく <see cref="IAnomalyService"/>。
/// </summary>
internal sealed class FakeAnomalyService : IAnomalyService
{
    public event EventHandler<AnomalyNotice>? AnomalyChanged;

    public List<AnomalyRecord> Anomalies { get; } = [];

    public List<AnomalySetting> Settings { get; } = [.. AnomalySettingRules.Kinds.Select(AnomalySettingRules.Default)];

    public List<(long Id, bool Dismissed)> DismissCalls { get; } = [];

    public List<long> NotifiedCalls { get; } = [];

    public (DateTimeOffset From, DateTimeOffset To, bool IncludeDismissed, int Limit)? LastQuery { get; private set; }

    public void Raise(AnomalyNotice notice) => AnomalyChanged?.Invoke(this, notice);

    public Task<IReadOnlyList<AnomalyRecord>> GetAnomaliesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        bool includeDismissed,
        int limit,
        CancellationToken cancellationToken = default)
    {
        LastQuery = (fromUtc, toUtc, includeDismissed, limit);
        return Task.FromResult<IReadOnlyList<AnomalyRecord>>(Anomalies.Take(limit).ToArray());
    }

    public Task<SensorSettingResult> SetDismissedAsync(long anomalyId, bool dismissed, CancellationToken cancellationToken = default)
    {
        DismissCalls.Add((anomalyId, dismissed));
        return Task.FromResult(Anomalies.Any(a => a.AnomalyId == anomalyId) ? SensorSettingResult.Applied : SensorSettingResult.NotFound);
    }

    public Task<SensorSettingResult> MarkNotifiedAsync(long anomalyId, CancellationToken cancellationToken = default)
    {
        NotifiedCalls.Add(anomalyId);
        return Task.FromResult(SensorSettingResult.Applied);
    }

    public Task<IReadOnlyList<AnomalySetting>> GetSettingsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AnomalySetting>>(Settings.ToArray());

    public Task<SensorSettingResult> SetSettingAsync(AnomalySetting setting, CancellationToken cancellationToken = default)
    {
        if (!AnomalySettingRules.IsValid(setting))
        {
            return Task.FromResult(SensorSettingResult.Invalid);
        }

        Settings.RemoveAll(s => s.Kind == setting.Kind);
        Settings.Add(setting);
        return Task.FromResult(SensorSettingResult.Applied);
    }
}

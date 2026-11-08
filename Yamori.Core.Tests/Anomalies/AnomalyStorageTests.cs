using Microsoft.Data.Sqlite;
using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Storage;
using Yamori.Core.Anomalies.Thresholds;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Master;

namespace Yamori.Core.Tests.Anomalies;

public sealed class AnomalyStorageTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private readonly MasterDatabase _master;
    private readonly SqliteAnomalyRepository _repository;

    public AnomalyStorageTests()
    {
        SqliteConnection.ClearAllPools();
        _master = MasterDatabase.Open(":memory:");
        _repository = new SqliteAnomalyRepository(_master);
    }

    public void Dispose() => _master.Dispose();

    [Fact]
    public void InsertedAnomalyRoundTripsWithItsEvidence()
    {
        var inserted = _repository.Insert(NewAnomaly(Start, new() { [AnomalyEvidenceNames.PeakTemperatureCelsius] = 96.5, ["broken"] = double.NaN }));

        var found = _repository.Find(inserted.AnomalyId);

        Assert.NotNull(found);
        Assert.True(found.AnomalyId > 0);
        Assert.Equal(AnomalyKind.ThermalThrottling, found.Kind);
        Assert.Equal("cpu/0", found.HardwareKey);
        Assert.Equal(HardwareKind.Cpu, found.HardwareKind);
        Assert.Equal("Intel Core", found.HardwareName);
        Assert.Equal(Start, found.StartedUtc);
        Assert.True(found.IsOngoing);
        Assert.Equal(96.5, found.Evidence[AnomalyEvidenceNames.PeakTemperatureCelsius]);
        Assert.False(found.Evidence.ContainsKey("broken"));
    }

    [Fact]
    public void UpdateAndEndChangeOnlyOngoingAnomalies()
    {
        var anomaly = _repository.Insert(NewAnomaly(Start));

        _repository.UpdateOngoing(anomaly.AnomalyId, Start.AddSeconds(30), AnomalySeverity.Warning, new Dictionary<string, double> { ["x"] = 1 });
        _repository.End(anomaly.AnomalyId, Start.AddMinutes(2), AnomalySeverity.Warning, new Dictionary<string, double> { ["x"] = 2 });
        _repository.UpdateOngoing(anomaly.AnomalyId, Start.AddMinutes(5), AnomalySeverity.Critical, new Dictionary<string, double>());

        var found = _repository.Find(anomaly.AnomalyId)!;
        Assert.Equal(Start.AddMinutes(2), found.EndedUtc);
        Assert.Equal(AnomalySeverity.Warning, found.Severity);
        Assert.Equal(2, found.Evidence["x"]);
        Assert.Equal(TimeSpan.FromMinutes(2), found.Duration);
    }

    [Fact]
    public void CloseOngoingEndsAnomaliesAtTheirLastSeenTime()
    {
        var anomaly = _repository.Insert(NewAnomaly(Start));
        _repository.UpdateOngoing(anomaly.AnomalyId, Start.AddMinutes(3), AnomalySeverity.Notice, new Dictionary<string, double>());

        Assert.Equal(1, _repository.CloseOngoing());
        Assert.Equal(Start.AddMinutes(3), _repository.Find(anomaly.AnomalyId)!.EndedUtc);
        Assert.Equal(0, _repository.CloseOngoing());
    }

    [Fact]
    public void OverlappingReturnsNewestFirstAndHidesDismissed()
    {
        var old = _repository.Insert(NewAnomaly(Start.AddHours(-30)));
        _repository.End(old.AnomalyId, Start.AddHours(-29), AnomalySeverity.Notice, new Dictionary<string, double>());
        var first = _repository.Insert(NewAnomaly(Start.AddHours(-2)));
        _repository.End(first.AnomalyId, Start.AddHours(-1), AnomalySeverity.Notice, new Dictionary<string, double>());
        var ongoing = _repository.Insert(NewAnomaly(Start));
        var dismissed = _repository.Insert(NewAnomaly(Start.AddMinutes(1)));
        _repository.SetDismissed(dismissed.AnomalyId, Start.AddMinutes(2));

        var visible = _repository.GetOverlapping(Start.AddHours(-24), Start.AddHours(1), includeDismissed: false, limit: 10);
        var all = _repository.GetOverlapping(Start.AddHours(-24), Start.AddHours(1), includeDismissed: true, limit: 10);
        var limited = _repository.GetOverlapping(Start.AddHours(-24), Start.AddHours(1), includeDismissed: true, limit: 1);

        Assert.Equal([ongoing.AnomalyId, first.AnomalyId], visible.Select(a => a.AnomalyId));
        Assert.Equal([dismissed.AnomalyId, ongoing.AnomalyId, first.AnomalyId], all.Select(a => a.AnomalyId));
        Assert.Equal([dismissed.AnomalyId], limited.Select(a => a.AnomalyId));
    }

    [Fact]
    public void DismissalCanBeUndone()
    {
        var anomaly = _repository.Insert(NewAnomaly(Start));

        Assert.True(_repository.SetDismissed(anomaly.AnomalyId, Start)!.IsDismissed);
        Assert.False(_repository.SetDismissed(anomaly.AnomalyId, null)!.IsDismissed);
        Assert.Null(_repository.SetDismissed(9999, Start));
    }

    [Fact]
    public void LastNotifiedIsPerKindAndHardware()
    {
        var cpu = _repository.Insert(NewAnomaly(Start));
        var gpu = _repository.Insert(NewAnomaly(Start) with { HardwareKey = "gpu/1", HardwareKind = HardwareKind.Gpu });

        Assert.Null(_repository.GetLastNotified(AnomalyKind.ThermalThrottling, "cpu/0"));
        Assert.True(_repository.MarkNotified(cpu.AnomalyId, Start.AddMinutes(1)));
        Assert.False(_repository.MarkNotified(9999, Start));

        Assert.Equal(Start.AddMinutes(1), _repository.GetLastNotified(AnomalyKind.ThermalThrottling, "cpu/0"));
        Assert.Null(_repository.GetLastNotified(AnomalyKind.ThermalThrottling, gpu.HardwareKey));
        Assert.Null(_repository.GetLastNotified(AnomalyKind.WorseThanUsual, "cpu/0"));
    }

    [Fact]
    public void SettingsDefaultToNotificationsOnAndAutomaticThresholds()
    {
        var repository = new SqliteAnomalySettingsRepository(_master);

        Assert.Equal(AnomalySettingRules.Kinds.Select(AnomalySettingRules.Default), repository.GetAll());

        repository.Save(new AnomalySetting(AnomalyKind.ThermalThrottling, false, 88));
        repository.Save(new AnomalySetting(AnomalyKind.ThermalThrottling, true, null));
        repository.Save(new AnomalySetting(AnomalyKind.WorseThanUsual, false, 10));

        Assert.Equal(
            [new AnomalySetting(AnomalyKind.ThermalThrottling, true, null), new AnomalySetting(AnomalyKind.WorseThanUsual, false, 10)],
            repository.GetAll());
    }

    [Fact]
    public void TheGlobalNotificationSwitchDefaultsToOnAndIsSaved()
    {
        var repository = new SqliteAnomalySettingsRepository(_master);
        Assert.True(repository.GetNotificationsEnabled());

        var cache = new AnomalySettingsCache(repository);
        cache.SaveNotificationsEnabled(false);
        Assert.False(cache.GetNotificationsEnabled());
        Assert.False(new SqliteAnomalySettingsRepository(_master).GetNotificationsEnabled());

        repository.SaveNotificationsEnabled(true);
        Assert.True(repository.GetNotificationsEnabled());
    }

    [Fact]
    public void SettingsCacheRejectsOutOfRangeThresholdsAndWritesThrough()
    {
        var repository = new SqliteAnomalySettingsRepository(_master);
        var cache = new AnomalySettingsCache(repository);

        Assert.Throws<ArgumentException>(() => cache.Save(new AnomalySetting(AnomalyKind.ThermalThrottling, true, 200)));
        cache.Save(new AnomalySetting(AnomalyKind.ThermalThrottling, true, 92));

        Assert.Equal(92, cache.Get(AnomalyKind.ThermalThrottling).ManualThreshold);
        Assert.Equal(92, repository.GetAll()[0].ManualThreshold);
    }

    [Theory]
    [InlineData(AnomalyKind.ThermalThrottling, null, true)]
    [InlineData(AnomalyKind.ThermalThrottling, 50.0, true)]
    [InlineData(AnomalyKind.ThermalThrottling, 110.0, true)]
    [InlineData(AnomalyKind.ThermalThrottling, 49.0, false)]
    [InlineData(AnomalyKind.ThermalThrottling, double.NaN, false)]
    [InlineData(AnomalyKind.WorseThanUsual, 2.0, true)]
    [InlineData(AnomalyKind.WorseThanUsual, 31.0, false)]
    [InlineData((AnomalyKind)99, null, false)]
    public void SettingRulesCheckTheThresholdRange(AnomalyKind kind, double? threshold, bool expected)
    {
        Assert.Equal(expected, AnomalySettingRules.IsValid(new AnomalySetting(kind, true, threshold)));
    }

    [Theory]
    [InlineData(HardwareKind.Cpu, 100.0, null, 95.0)]   // TjMax の 5℃ 下
    [InlineData(HardwareKind.Cpu, null, null, 90.0)]    // TjMax が取れなければ CPU の初期値
    [InlineData(HardwareKind.Gpu, null, null, 85.0)]
    [InlineData(HardwareKind.Cpu, 100.0, 80.0, 80.0)]   // 手動の指定が最優先
    public void TemperatureThresholdUsesTjMaxThenTheDefault(HardwareKind kind, double? tjMax, double? manual, double expected)
    {
        Assert.Equal(expected, TemperatureThresholds.Resolve(kind, tjMax, manual));
    }

    [Fact]
    public void EvidenceJsonIgnoresBrokenText()
    {
        Assert.Empty(AnomalyEvidenceJson.Deserialize("not json"));
        Assert.Empty(AnomalyEvidenceJson.Deserialize(null));
        Assert.Equal(1.5, AnomalyEvidenceJson.Deserialize(AnomalyEvidenceJson.Serialize(new Dictionary<string, double> { ["a"] = 1.5 }))["a"]);
    }

    private static AnomalyRecord NewAnomaly(DateTimeOffset started, Dictionary<string, double>? evidence = null) =>
        new(0, AnomalyKind.ThermalThrottling, "cpu/0", HardwareKind.Cpu, "Intel Core", started, started, null, AnomalySeverity.Notice,
            evidence ?? [], null, null);
}

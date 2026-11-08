using Microsoft.Data.Sqlite;
using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Detection;
using Yamori.Core.Anomalies.Service;
using Yamori.Core.Anomalies.Storage;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Master;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Anomalies;

public sealed class AnomalyDetectorTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly SensorKey Temperature = SensorKey.Parse("cpu/0/temperature/package");

    private readonly MasterDatabase _master;
    private readonly SqliteAnomalyRepository _repository;
    private readonly AnomalySettingsCache _settings;
    private readonly ManualClock _clock = new() { Now = Start };
    private readonly ScriptedAnomalyRule _rule = new();
    private readonly AnomalyDetector _detector;
    private readonly List<AnomalyNotice> _notices = [];

    public AnomalyDetectorTests()
    {
        SqliteConnection.ClearAllPools();
        _master = MasterDatabase.Open(":memory:");
        _repository = new SqliteAnomalyRepository(_master);
        _settings = new AnomalySettingsCache(new SqliteAnomalySettingsRepository(_master));
        _detector = new AnomalyDetector([_rule], _repository, _settings, new AnomalyNotificationPolicy(), _clock, TimeSpan.FromSeconds(30));
        _detector.AnomalyChanged += (_, n) => _notices.Add(n);
        _detector.OnCatalogChanged([new SensorDescriptor(Temperature, SensorUnit.Celsius, "CPU Package", "Intel Core i7", "temperature", "pawnio")]);
    }

    public void Dispose() => _master.Dispose();

    [Fact]
    public void RulesSeeTheLatestValuesInMemory()
    {
        Tick(seconds: 0, temperature: 97);

        Assert.Equal(97, _rule.LastFrame!.Value(Temperature));
        Assert.Equal(AnomalyKind.ThermalThrottling, _rule.LastFrame.Setting.Kind);

        // 古くなった値（5 秒より前）は今の値として使わない。
        _clock.Now = Start.AddSeconds(10);
        _detector.OnReadings([]);
        Assert.Null(_rule.LastFrame!.Value(Temperature));
    }

    [Fact]
    public void AnEpisodeStartsContinuesAndEnds()
    {
        _rule.Findings.Add(Finding(AnomalySeverity.Notice, temperature: 96, since: Start.AddSeconds(-10)));
        Tick(0);
        var started = Assert.Single(_notices);
        Assert.Equal(AnomalyChange.Started, started.Change);
        Assert.True(started.ShouldNotify);
        Assert.Equal("Intel Core i7", started.Anomaly.HardwareName);
        Assert.Equal(Start.AddSeconds(-10), started.Anomaly.StartedUtc);

        // 続いている間は知らせず、毎秒は書かない。
        Tick(10);
        Assert.Single(_notices);
        Assert.Equal(Start, _repository.Find(started.Anomaly.AnomalyId)!.LastSeenUtc);

        // 書く間隔（30 秒）が来たら、最後に確かめた時刻を書く。
        Tick(30);
        Assert.Equal(Start.AddSeconds(30), _repository.Find(started.Anomaly.AnomalyId)!.LastSeenUtc);

        _rule.Findings.Clear();
        Tick(40);
        var ended = _notices[^1];
        Assert.Equal(AnomalyChange.Ended, ended.Change);
        Assert.False(ended.ShouldNotify);
        Assert.Equal(Start.AddSeconds(40), _repository.Find(started.Anomaly.AnomalyId)!.EndedUtc);
    }

    [Fact]
    public void EscalationIsWrittenAndAnnouncedButSeverityNeverDrops()
    {
        _rule.Findings.Add(Finding(AnomalySeverity.Notice));
        Tick(0);
        _rule.Findings[0] = Finding(AnomalySeverity.Critical, temperature: 101);
        Tick(1);
        _rule.Findings[0] = Finding(AnomalySeverity.Notice);
        Tick(2);

        Assert.Equal([AnomalyChange.Started, AnomalyChange.Escalated], _notices.Select(n => n.Change));
        var stored = _repository.Find(_notices[0].Anomaly.AnomalyId)!;
        Assert.Equal(AnomalySeverity.Critical, stored.Severity);
        Assert.Equal(101, stored.Evidence[AnomalyEvidenceNames.PeakTemperatureCelsius]);
    }

    [Fact]
    public async Task SameAnomalyIsNotifiedAtMostOnceInSixHours()
    {
        var service = new LocalAnomalyService(_detector, _repository, _settings, _clock);
        _rule.Findings.Add(Finding(AnomalySeverity.Notice));
        Tick(0);
        Assert.Equal(SensorSettingResult.Applied, await service.MarkNotifiedAsync(_notices[0].Anomaly.AnomalyId));

        // 通知済みの異常が重くなっても、もう一度は通知しない。
        _rule.Findings[0] = Finding(AnomalySeverity.Warning);
        Tick(1);
        Assert.False(_notices[^1].ShouldNotify);

        // 終わってすぐ（6 時間以内）にまた起きても通知しない。
        _rule.Findings.Clear();
        Tick(2);
        _rule.Findings.Add(Finding(AnomalySeverity.Notice));
        Tick(60);
        Assert.Equal(AnomalyChange.Started, _notices[^1].Change);
        Assert.False(_notices[^1].ShouldNotify);

        // 6 時間たてば通知する。
        _rule.Findings.Clear();
        Tick(61);
        _rule.Findings.Add(Finding(AnomalySeverity.Notice));
        Tick((int)TimeSpan.FromHours(6).TotalSeconds + 1);
        Assert.True(_notices[^1].ShouldNotify);
    }

    [Fact]
    public async Task NotificationsCanBeTurnedOffAndDismissedAnomaliesStayQuiet()
    {
        _settings.Save(new AnomalySetting(AnomalyKind.ThermalThrottling, NotificationsEnabled: false, ManualThreshold: null));
        _rule.Findings.Add(Finding(AnomalySeverity.Notice));
        Tick(0);
        Assert.False(_notices[^1].ShouldNotify);

        _settings.Save(AnomalySettingRules.Default(AnomalyKind.ThermalThrottling));
        var service = new LocalAnomalyService(_detector, _repository, _settings, _clock);
        var dismissedNotices = new List<AnomalyNotice>();
        service.AnomalyChanged += (_, n) => dismissedNotices.Add(n);
        Assert.Equal(SensorSettingResult.Applied, await service.SetDismissedAsync(_notices[0].Anomaly.AnomalyId, dismissed: true));
        Assert.Equal(AnomalyChange.DismissalChanged, Assert.Single(dismissedNotices).Change);

        _rule.Findings[0] = Finding(AnomalySeverity.Critical);
        Tick(1);
        Assert.Equal(AnomalyChange.Escalated, _notices[^1].Change);
        Assert.False(_notices[^1].ShouldNotify);
    }

    [Fact]
    public void AnomaliesOutsideTheNotificationScopeStayInTheListUntilTheyBecomeNotifiable()
    {
        _rule.Findings.Add(Finding(AnomalySeverity.Notice) with { Notifiable = false });
        Tick(0);
        Tick(1);

        Assert.Equal(AnomalyChange.Started, Assert.Single(_notices).Change);
        Assert.False(_notices[0].ShouldNotify);

        // 重さが同じでも、通知の対象になったら知らせる（1 回だけ）。
        _rule.Findings[0] = Finding(AnomalySeverity.Notice);
        Tick(2);
        Tick(3);
        _rule.Findings[0] = Finding(AnomalySeverity.Notice) with { Notifiable = false };
        Tick(4);

        Assert.Equal([AnomalyChange.Started, AnomalyChange.Escalated], _notices.Select(n => n.Change));
        Assert.True(_notices[1].ShouldNotify);
        Assert.Equal(_notices[0].Anomaly.AnomalyId, _notices[1].Anomaly.AnomalyId);
    }

    [Fact]
    public void AFailingRuleDoesNotStopDetectionOrEndAnomalies()
    {
        _rule.Findings.Add(Finding(AnomalySeverity.Notice));
        Tick(0);
        _rule.Throw = true;

        Tick(1);

        Assert.Single(_notices);
        Assert.True(_repository.Find(_notices[0].Anomaly.AnomalyId)!.IsOngoing);
    }

    [Fact]
    public void StartClosesAnomaliesLeftOpenByThePreviousRun()
    {
        _rule.Findings.Add(Finding(AnomalySeverity.Notice));
        Tick(0);

        var restarted = new AnomalyDetector([new ScriptedAnomalyRule()], _repository, _settings, new AnomalyNotificationPolicy(), _clock);

        Assert.Equal(1, restarted.Start());
        Assert.Equal(Start, _repository.Find(_notices[0].Anomaly.AnomalyId)!.EndedUtc);
    }

    [Fact]
    public void DuplicateRuleKindsAreRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new AnomalyDetector([new ScriptedAnomalyRule(), new ScriptedAnomalyRule()], _repository, _settings, new AnomalyNotificationPolicy(), _clock));
    }

    [Fact]
    public async Task ServiceValidatesSettingsAndListsAnomalies()
    {
        var service = new LocalAnomalyService(_detector, _repository, _settings, _clock);
        _rule.Findings.Add(Finding(AnomalySeverity.Notice));
        Tick(0);

        Assert.Equal(SensorSettingResult.Invalid, await service.SetSettingAsync(new AnomalySetting(AnomalyKind.ThermalThrottling, true, 5)));
        Assert.Equal(SensorSettingResult.Applied, await service.SetSettingAsync(new AnomalySetting(AnomalyKind.ThermalThrottling, true, 90)));
        Assert.Equal(90, (await service.GetSettingsAsync())[0].ManualThreshold);
        Assert.Single(await service.GetAnomaliesAsync(Start.AddHours(-1), Start.AddHours(1), includeDismissed: false, limit: 10));
        Assert.Equal(SensorSettingResult.NotFound, await service.SetDismissedAsync(9999, true));
    }

    [Fact]
    public async Task ServiceWithoutStorageIsEmptyAndUnavailable()
    {
        var service = new LocalAnomalyService(null, null, null, _clock);

        Assert.False(service.IsAvailable);
        Assert.Empty(await service.GetAnomaliesAsync(Start.AddHours(-1), Start, false, 10));
        Assert.Empty(await service.GetSettingsAsync());
        Assert.Equal(SensorSettingResult.Unavailable, await service.SetDismissedAsync(1, true));
        Assert.Equal(SensorSettingResult.Unavailable, await service.SetSettingAsync(AnomalySettingRules.Default(AnomalyKind.ThermalThrottling)));
    }

    private void Tick(int seconds, double temperature = 90)
    {
        _clock.Now = Start.AddSeconds(seconds);
        _detector.OnReadings([SensorReading.Ok(Temperature, temperature, _clock.Now, "pawnio")]);
    }

    private static AnomalyFinding Finding(AnomalySeverity severity, double temperature = 96, DateTimeOffset? since = null) =>
        new("cpu/0", HardwareKind.Cpu, severity, new Dictionary<string, double> { [AnomalyEvidenceNames.PeakTemperatureCelsius] = temperature }, since);
}

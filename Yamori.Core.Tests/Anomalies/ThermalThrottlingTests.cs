using Microsoft.Data.Sqlite;
using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Detection;
using Yamori.Core.Anomalies.Detection.Throttling;
using Yamori.Core.Anomalies.Storage;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Master;
using Yamori.Core.Sensors.Providers.PawnIo;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Anomalies;

public class ThermalThrottlingTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly ThrottlingSignals Quiet = new(60, 100, 100, false, false, false, false, 20, 4_000_000_000);
    private static readonly ThrottlingSignals Thermal = Quiet with { ThermalLimit = true, TemperatureCelsius = 99, ClockHertz = 2_800_000_000 };
    private static readonly AnomalySetting Automatic = AnomalySettingRules.Default(AnomalyKind.ThermalThrottling);

    [Fact]
    public void DecoderReadsTheTccActivationOffset()
    {
        var target = (100UL << 16) | (10UL << 24);

        Assert.Equal(10, IntelMsrDecoder.TccActivationOffset(target));
        Assert.Equal(90, IntelMsrDecoder.ThrottleTemperature(target));
        Assert.Null(IntelMsrDecoder.ThrottleTemperature(10UL << 24));
    }

    [Fact]
    public void SamplerPublishesTheThrottleTemperature()
    {
        var reader = FakeMsrReader.AlderLake();
        reader.PerCore[0][IntelMsrRegisters.TemperatureTarget] = (100UL << 16) | (15UL << 24);
        var sampler = new IntelMsrSensorSampler(reader, reader.Cores, new ManualClock());
        var catalog = sampler.Discover("pawnio");

        var reading = Assert.Single(sampler.Read([IntelMsrSensorSampler.ThrottleTemperatureKey], "pawnio"));

        Assert.Contains(catalog, d => d.Key == IntelMsrSensorSampler.ThrottleTemperatureKey);
        Assert.Equal(85, reading.Value);
    }

    [Fact]
    public void ThermalLimitIsReportedAfterTenSecondsAndEscalates()
    {
        var tracker = new ThrottlingEpisodeTracker("cpu/0", HardwareKind.Cpu);

        for (var s = 0; s < 9; s++)
        {
            Assert.Null(tracker.Update(Start.AddSeconds(s), Thermal, 95));
        }

        var first = tracker.Update(Start.AddSeconds(9), Thermal, 95);
        Assert.NotNull(first);
        Assert.Equal(AnomalySeverity.Notice, first.Severity);
        Assert.Equal(Start, first.SinceUtc);
        Assert.Equal(10, first.Evidence[AnomalyEvidenceNames.ThermalLimitSeconds]);
        Assert.Equal(99, first.Evidence[AnomalyEvidenceNames.PeakTemperatureCelsius]);
        Assert.Equal(95, first.Evidence[AnomalyEvidenceNames.ThresholdCelsius]);
        Assert.Equal(100, first.Evidence[AnomalyEvidenceNames.TjMaxCelsius]);

        AnomalyFinding? last = null;
        for (var s = 10; s < 60; s++)
        {
            last = tracker.Update(Start.AddSeconds(s), Thermal, 95);
        }

        Assert.Equal(AnomalySeverity.Warning, last!.Severity);
        Assert.Equal(AnomalySeverity.Critical, tracker.Update(Start.AddSeconds(60), Thermal with { CriticalTemperature = true }, 95)!.Severity);
    }

    [Fact]
    public void ShortInterruptionsBeforeReportingRestartTheCount()
    {
        var tracker = new ThrottlingEpisodeTracker("cpu/0", HardwareKind.Cpu);
        for (var s = 0; s < 6; s++)
        {
            tracker.Update(Start.AddSeconds(s), Thermal, 95);
        }

        // 5 秒までの途切れは続きとみなす。
        for (var s = 6; s < 10; s++)
        {
            Assert.Null(tracker.Update(Start.AddSeconds(s), Quiet, 95));
        }

        // 途切れる前の 6 秒と合わせて 10 秒になったところで異常。始まりは最初の時刻。
        for (var s = 10; s < 13; s++)
        {
            Assert.Null(tracker.Update(Start.AddSeconds(s), Thermal, 95));
        }

        Assert.Equal(Start, tracker.Update(Start.AddSeconds(13), Thermal, 95)!.SinceUtc);

        // 6 秒以上途切れたら数え直す。
        var other = new ThrottlingEpisodeTracker("cpu/0", HardwareKind.Cpu);
        for (var s = 0; s < 6; s++)
        {
            other.Update(Start.AddSeconds(s), Thermal, 95);
        }

        for (var s = 6; s < 13; s++)
        {
            other.Update(Start.AddSeconds(s), Quiet, 95);
        }

        for (var s = 13; s < 18; s++)
        {
            Assert.Null(other.Update(Start.AddSeconds(s), Thermal, 95));
        }
    }

    [Fact]
    public void AReportedEpisodeEndsOnlyAfterTheReleaseDelay()
    {
        var tracker = new ThrottlingEpisodeTracker("cpu/0", HardwareKind.Cpu);
        for (var s = 0; s < 10; s++)
        {
            tracker.Update(Start.AddSeconds(s), Thermal, 95);
        }

        Assert.NotNull(tracker.Update(Start.AddSeconds(30), Quiet, 95));
        Assert.NotNull(tracker.Update(Start.AddSeconds(39), Quiet, 95));
        Assert.Null(tracker.Update(Start.AddSeconds(40), Quiet, 95));
    }

    [Fact]
    public void PowerAndCurrentLimitsNeedAMinute()
    {
        var tracker = new ThrottlingEpisodeTracker("cpu/0", HardwareKind.Cpu);
        var limited = Quiet with { CurrentLimit = true };

        for (var s = 0; s < 59; s++)
        {
            Assert.Null(tracker.Update(Start.AddSeconds(s), limited, 95));
        }

        var finding = tracker.Update(Start.AddSeconds(59), limited, 95);
        Assert.NotNull(finding);
        Assert.Equal(AnomalySeverity.Notice, finding.Severity);
        Assert.Equal(60, finding.Evidence[AnomalyEvidenceNames.CurrentLimitSeconds]);
        Assert.Equal(0, finding.Evidence[AnomalyEvidenceNames.ThermalLimitSeconds]);

        // 電力・電流の制限だけで、クロックも下がっていなければ、一覧にだけ残す。
        Assert.False(finding.Notifiable);
    }

    [Fact]
    public void PowerLimitWithAClockDropOrHeatIsNotifiable()
    {
        var dropped = new ThrottlingEpisodeTracker("cpu/0", HardwareKind.Cpu);
        for (var s = 0; s < 60; s++)
        {
            dropped.Update(Start.AddSeconds(s), Quiet, 95);
        }

        AnomalyFinding? finding = null;
        for (var s = 60; s < 120; s++)
        {
            finding = dropped.Update(Start.AddSeconds(s), Quiet with { PowerLimit = true, ClockHertz = 3_500_000_000 }, 95);
        }

        Assert.Equal(12.5, finding!.Evidence[AnomalyEvidenceNames.ClockDropPercent]);
        Assert.True(finding.Notifiable);

        // 電流の制限が続いたあとで熱の制限が 10 秒続いたら、通知の対象になる。
        var heated = new ThrottlingEpisodeTracker("cpu/0", HardwareKind.Cpu);
        var limited = Quiet with { CurrentLimit = true };
        for (var s = 0; s < 60; s++)
        {
            finding = heated.Update(Start.AddSeconds(s), limited, 95);
        }

        Assert.False(finding!.Notifiable);
        for (var s = 60; s < 70; s++)
        {
            finding = heated.Update(Start.AddSeconds(s), limited with { ThermalLimit = true }, 95);
        }

        Assert.True(finding!.Notifiable);
        Assert.True(new ThrottlingEpisodeTracker("cpu/0", HardwareKind.Cpu).Update(Start, Thermal with { CriticalTemperature = true }, 95)!.Notifiable);
    }

    [Fact]
    public void HighTemperatureAloneNeedsThirtySeconds()
    {
        var tracker = new ThrottlingEpisodeTracker("gpu/1", HardwareKind.Gpu);
        var hot = Quiet with { TemperatureCelsius = 88 };

        for (var s = 0; s < 29; s++)
        {
            Assert.Null(tracker.Update(Start.AddSeconds(s), hot, 85));
        }

        Assert.NotNull(tracker.Update(Start.AddSeconds(29), hot, 85));
    }

    [Fact]
    public void ClockDropIsMeasuredAgainstTheClockBeforeTheEpisode()
    {
        var tracker = new ThrottlingEpisodeTracker("cpu/0", HardwareKind.Cpu);
        for (var s = 0; s < 60; s++)
        {
            tracker.Update(Start.AddSeconds(s), Quiet, 95);
        }

        AnomalyFinding? finding = null;
        for (var s = 60; s < 70; s++)
        {
            finding = tracker.Update(Start.AddSeconds(s), Thermal, 95);
        }

        Assert.Equal(4_000_000_000, finding!.Evidence[AnomalyEvidenceNames.BaselineClockHertz]);
        Assert.Equal(2_800_000_000, finding.Evidence[AnomalyEvidenceNames.ClockHertz]);
        Assert.Equal(30, finding.Evidence[AnomalyEvidenceNames.ClockDropPercent]);
        Assert.Equal(AnomalySeverity.Warning, finding.Severity); // 25% 以上の低下
    }

    [Fact]
    public void BaselineForgetsOldClocks()
    {
        var baseline = new ClockBaseline(TimeSpan.FromMinutes(10));
        baseline.Add(Start, 5e9);
        baseline.Add(Start.AddMinutes(5), 4e9);

        Assert.Equal(5e9, baseline.Max(Start.AddMinutes(6)));
        Assert.Equal(4e9, baseline.Max(Start.AddMinutes(11)));
        Assert.Null(baseline.Max(Start.AddMinutes(30)));
    }

    [Fact]
    public void CpuSourceReadsMsrFlagsAndTheThrottleTemperature()
    {
        var frame = Frame(Start, Automatic,
            ("cpu/0/temperature/package", 91), ("cpu/0/temperature/tjmax", 100), ("cpu/0/temperature/throttle-target", 90),
            ("cpu/0/flag/cores-thermal-throttling", 1), ("cpu/0/flag/power-limit", 0), ("cpu/0/flag/cores-current-limit", 1),
            ("cpu/0/load/total", 80), ("cpu/0/clock/average-effective", 3.1e9), ("cpu/0/clock/average-estimated", 4e9));

        var observation = Assert.Single(new CpuThrottlingSignalSource().Read(frame));

        Assert.Equal("cpu/0", observation.HardwareKey);
        Assert.Equal(new ThrottlingSignals(91, 90, 100, true, false, false, true, 80, 3.1e9), observation.Signals);
    }

    [Fact]
    public void CpuSourceFallsBackToTheWindowsPerformanceLimit()
    {
        var frame = Frame(Start, Automatic, ("cpu/0/level/performance-limit", 70), ("cpu/0/load/total", 90), ("cpu/0/clock/average-estimated", 2e9));

        var signals = Assert.Single(new CpuThrottlingSignalSource().Read(frame)).Signals;

        Assert.True(signals.PowerLimit);
        Assert.False(signals.ThermalLimit);
        Assert.Equal(2e9, signals.ClockHertz);
        Assert.Empty(new CpuThrottlingSignalSource().Read(Frame(Start, Automatic, ("cpu/0/load/total", 90))));
    }

    [Fact]
    public void ThermalZoneSourceTreatsThePassiveLimitAsThermal()
    {
        var frame = Frame(Start, Automatic,
            ("thermal-zone/aa/temperature/zone", 70), ("thermal-zone/aa/level/passive-limit", 60),
            ("thermal-zone/bb/temperature/zone", 50), ("thermal-zone/bb/flag/throttling", 0), ("thermal-zone/bb/level/passive-limit", 100));

        var observations = new ThermalZoneThrottlingSignalSource().Read(frame).ToDictionary(o => o.HardwareKey);

        Assert.True(observations["thermal-zone/aa"].Signals.ThermalLimit);
        Assert.False(observations["thermal-zone/bb"].Signals.ThermalLimit);
    }

    [Fact]
    public void GpuSourceUsesTheDriverTemperatureLimit()
    {
        var frame = Frame(Start, Automatic, ("gpu/1/temperature/adapter", 80), ("gpu/1/temperature/adapter-max", 87), ("gpu/1/clock/core", 1.5e9), ("gpu/1/load/max-engine", 99));

        var observation = Assert.Single(new GpuThrottlingSignalSource().Read(frame));

        Assert.Equal("gpu/1", observation.HardwareKey);
        Assert.Equal(87, observation.Signals.ReportedLimitCelsius);
        Assert.Equal(1.5e9, observation.Signals.ClockHertz);
    }

    [Fact]
    public void RuleUsesTheThrottleTemperatureOrTheManualThreshold()
    {
        var rule = new ThermalThrottlingRule();
        var manual = new AnomalySetting(AnomalyKind.ThermalThrottling, true, 80);

        // 熱制御は 90℃ から → 注意は 85℃。86℃ が 30 秒続けば異常。
        for (var s = 0; s < 30; s++)
        {
            var findings = rule.Evaluate(Frame(Start.AddSeconds(s), Automatic, ("cpu/0/temperature/package", 86), ("cpu/0/temperature/throttle-target", 90)));
            Assert.Equal(s == 29, findings.Count == 1);
        }

        Assert.Equal(85, rule.Evaluate(Frame(Start.AddSeconds(30), Automatic, ("cpu/0/temperature/package", 86), ("cpu/0/temperature/throttle-target", 90)))[0]
            .Evidence[AnomalyEvidenceNames.ThresholdCelsius]);

        var manualRule = new ThermalThrottlingRule();
        for (var s = 0; s < 29; s++)
        {
            manualRule.Evaluate(Frame(Start.AddSeconds(s), manual, ("cpu/0/temperature/package", 82)));
        }

        Assert.Equal(80, Assert.Single(manualRule.Evaluate(Frame(Start.AddSeconds(29), manual, ("cpu/0/temperature/package", 82))))
            .Evidence[AnomalyEvidenceNames.ThresholdCelsius]);
    }

    [Fact]
    public void RuleKeepsAnEpisodeWhenValuesBrieflyDisappear()
    {
        var rule = new ThermalThrottlingRule();
        for (var s = 0; s < 10; s++)
        {
            rule.Evaluate(Frame(Start.AddSeconds(s), Automatic, ("cpu/0/flag/thermal-throttling", 1)));
        }

        // CPU の値が 1 回読めなくても、異常は続いている。
        Assert.Single(rule.Evaluate(Frame(Start.AddSeconds(10), Automatic)));
        Assert.Empty(rule.Evaluate(Frame(Start.AddSeconds(50), Automatic)));
    }

    [Fact]
    public void DetectorWithTheRuleRecordsAThrottlingEpisode()
    {
        SqliteConnection.ClearAllPools();
        using var master = MasterDatabase.Open(":memory:");
        var repository = new SqliteAnomalyRepository(master);
        var clock = new ManualClock { Now = Start };
        var detector = new AnomalyDetector(
            [new ThermalThrottlingRule()],
            repository,
            new AnomalySettingsCache(new SqliteAnomalySettingsRepository(master)),
            new AnomalyNotificationPolicy(),
            clock);
        var notices = new List<AnomalyNotice>();
        detector.AnomalyChanged += (_, n) => notices.Add(n);
        detector.Start();

        for (var s = 0; s < 12; s++)
        {
            clock.Now = Start.AddSeconds(s);
            detector.OnReadings(
            [
                SensorReading.Ok(SensorKey.Parse("cpu/0/flag/thermal-throttling"), 1, clock.Now, "pawnio"),
                SensorReading.Ok(SensorKey.Parse("cpu/0/temperature/package"), 100, clock.Now, "pawnio"),
            ]);
        }

        var started = Assert.Single(notices);
        Assert.Equal(AnomalyChange.Started, started.Change);
        Assert.True(started.ShouldNotify);
        Assert.Equal(Start, started.Anomaly.StartedUtc);
        Assert.Equal(AnomalyKind.ThermalThrottling, started.Anomaly.Kind);
        Assert.Equal("cpu/0", started.Anomaly.HardwareKey);
        Assert.Equal(100, repository.Find(started.Anomaly.AnomalyId)!.Evidence[AnomalyEvidenceNames.PeakTemperatureCelsius]);
    }

    private static AnomalyFrame Frame(DateTimeOffset at, AnomalySetting setting, params (string Key, double Value)[] values) =>
        new(at, values.ToDictionary(v => SensorKey.Parse(v.Key), v => SensorReading.Ok(SensorKey.Parse(v.Key), v.Value, at, "test")), setting);
}

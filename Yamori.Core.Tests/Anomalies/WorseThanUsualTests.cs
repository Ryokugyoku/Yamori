using Microsoft.Data.Sqlite;
using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Detection;
using Yamori.Core.Anomalies.Detection.Usual;
using Yamori.Core.Anomalies.Storage;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Master;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Anomalies;

public sealed class WorseThanUsualTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RecentEnd = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly SensorKey CpuLoad = SensorKey.Parse("cpu/0/load/total");
    private static readonly SensorKey CpuTemperature = SensorKey.Parse("cpu/0/temperature/package");
    private static readonly UsualComparisonOptions Options = UsualComparisonOptions.Default;

    private readonly MasterDatabase _master;
    private readonly SqliteAnomalyRepository _repository;

    public WorseThanUsualTests()
    {
        SqliteConnection.ClearAllPools();
        _master = MasterDatabase.Open(":memory:");
        _repository = new SqliteAnomalyRepository(_master);
    }

    public void Dispose() => _master.Dispose();

    [Fact]
    public void NothingIsJudgedUntilThereAreSevenDaysOfRecords()
    {
        // 6 日分しかない。直近 3 時間はふだんより 20℃ 高いが、判定しない。
        var minutes = History(Now - TimeSpan.FromDays(6), Now, t => t >= RecentEnd.AddHours(-3) ? (2, 60) : Usual(t));

        var result = UsualComparisonAnalyzer.Analyze(minutes, Now, Options.TemperatureDeltaCelsius, compareIdleLoad: true, Options);

        Assert.False(result.HasEnoughHistory);
        Assert.Null(result.Temperature);
        Assert.Null(result.IdleLoad);

        // 7 日分を超えたら判定する。
        var longer = History(Now - TimeSpan.FromDays(8), Now, t => t >= RecentEnd.AddHours(-3) ? (2, 60) : Usual(t));
        Assert.NotNull(UsualComparisonAnalyzer.Analyze(longer, Now, 8, compareIdleLoad: true, Options).Temperature);
    }

    [Fact]
    public void TemperatureIsComparedWithinTheSameLoadBand()
    {
        // ふだん：負荷 2% のときは 40℃、負荷 25% のときは 55℃（全体の中央値は 47.5℃）。
        // 直近 3 時間は負荷 25% で 56℃。全体の中央値と比べれば 8.5℃ 高いが、同じ帯（20〜30%）と比べれば 1℃ しか違わない。
        var sameBand = History(Now - TimeSpan.FromDays(14), Now, t => t >= RecentEnd.AddHours(-3) ? (25, 56) : Usual(t));
        Assert.Null(UsualComparisonAnalyzer.Analyze(sameBand, Now, 8, compareIdleLoad: false, Options).Temperature);

        // 負荷 2% で 50℃ なら、同じ帯のふだん（40℃）より 10℃ 高い。
        var hotter = History(Now - TimeSpan.FromDays(14), Now, t => t >= RecentEnd.AddHours(-3) ? (2, 50) : Usual(t));
        var run = UsualComparisonAnalyzer.Analyze(hotter, Now, 8, compareIdleLoad: false, Options).Temperature;

        Assert.NotNull(run);
        Assert.Equal(40, run!.Baseline, 3);
        Assert.Equal(10, run.Delta, 3);

        // 30% 以上の負荷の時間は「待機中」でないので比べない。
        var busy = History(Now - TimeSpan.FromDays(14), Now, t => t >= RecentEnd.AddHours(-3) ? (80, 90) : Usual(t));
        Assert.Null(UsualComparisonAnalyzer.Analyze(busy, Now, 8, compareIdleLoad: false, Options).Temperature);
    }

    [Fact]
    public void ShortPeaksAreIgnored()
    {
        // 高いのが 2 時間だけなら、続いた時間の下限（3 時間）に満たない。
        var twoHours = History(Now - TimeSpan.FromDays(14), Now, t => t >= RecentEnd.AddHours(-2) ? (2, 52) : Usual(t));
        Assert.Null(UsualComparisonAnalyzer.Analyze(twoHours, Now, 8, compareIdleLoad: true, Options).Temperature);

        var threeHours = History(Now - TimeSpan.FromDays(14), Now, t => t >= RecentEnd.AddHours(-3) ? (2, 52) : Usual(t));
        var run = UsualComparisonAnalyzer.Analyze(threeHours, Now, 8, compareIdleLoad: true, Options).Temperature;

        Assert.NotNull(run);
        Assert.Equal(RecentEnd.AddHours(-3), run!.StartUtc);
        Assert.Equal(RecentEnd, run.EndUtc);
        Assert.Equal(TimeSpan.FromHours(3), run.ElevatedDuration);

        // 差がしきい値に届かなければ（7℃ < 8℃）異常にしない。
        var slightly = History(Now - TimeSpan.FromDays(14), Now, t => t >= RecentEnd.AddHours(-5) ? (2, 47) : Usual(t));
        Assert.Null(UsualComparisonAnalyzer.Analyze(slightly, Now, 8, compareIdleLoad: true, Options).Temperature);
    }

    [Fact]
    public void HoursWithoutIdleTimeAreSkippedButANormalHourBreaksTheRun()
    {
        // 8〜9 時と 10〜12 時が高く、9〜10 時は記録なし（電源が切れていた）。判定できない区切りは挟んでも続き。
        var gap = History(Now - TimeSpan.FromDays(14), Now, t =>
            t >= RecentEnd.AddHours(-4) && t < RecentEnd.AddHours(-3) ? (2, 52)
            : t >= RecentEnd.AddHours(-3) && t < RecentEnd.AddHours(-2) ? null
            : t >= RecentEnd.AddHours(-2) ? (2, 52)
            : Usual(t));
        var run = UsualComparisonAnalyzer.Analyze(gap, Now, 8, compareIdleLoad: false, Options).Temperature;

        Assert.NotNull(run);
        Assert.Equal(RecentEnd.AddHours(-4), run!.StartUtc);
        Assert.Equal(TimeSpan.FromHours(3), run.ElevatedDuration);

        // 9〜10 時がふつうの温度なら、そこで切れて 2 時間しか続いていない。
        var broken = History(Now - TimeSpan.FromDays(14), Now, t =>
            t >= RecentEnd.AddHours(-4) && t < RecentEnd.AddHours(-3) ? (2, 52)
            : t >= RecentEnd.AddHours(-3) && t < RecentEnd.AddHours(-2) ? (2, 40)
            : t >= RecentEnd.AddHours(-2) ? (2, 52)
            : Usual(t));
        Assert.Null(UsualComparisonAnalyzer.Analyze(broken, Now, 8, compareIdleLoad: false, Options).Temperature);
    }

    [Fact]
    public void IdleLoadRisesWhenSomethingKeepsRunningInTheBackground()
    {
        // ふだんのいちばん静かな時間の負荷は 2%。直近 4 時間は 15〜18% から下がらない。
        var minutes = History(Now - TimeSpan.FromDays(14), Now, t => t >= RecentEnd.AddHours(-4) ? (15 + (t.Minute % 4), null) : Usual(t));

        var result = UsualComparisonAnalyzer.Analyze(minutes, Now, 8, compareIdleLoad: true, Options);

        Assert.NotNull(result.IdleLoad);
        Assert.Equal(2, result.IdleLoad!.Baseline, 3);
        Assert.True(result.IdleLoad.Delta >= 13, $"差は {result.IdleLoad.Delta}");
        Assert.Null(result.Temperature);

        // GPU など、待機中の負荷を比べない部品では見ない。
        Assert.Null(UsualComparisonAnalyzer.Analyze(minutes, Now, 8, compareIdleLoad: false, Options).IdleLoad);
    }

    [Fact]
    public void TemperatureAndIdleLoadBecomeOneFindingWithTheEvidence()
    {
        var part = UsualPartDefinitions.From([CpuLoad, CpuTemperature]).Single();
        var temperature = new ElevatedRun(RecentEnd.AddHours(-3), RecentEnd, TimeSpan.FromHours(3), 51.2, 42.04);
        var load = new ElevatedRun(RecentEnd.AddHours(-5), RecentEnd, TimeSpan.FromHours(5), 25, 3);

        var finding = UsualFindingFactory.Create(part, new UsualComparisonResult(true, temperature, load), 8, Options);

        Assert.NotNull(finding);
        Assert.Equal("cpu/0", finding!.HardwareKey);
        Assert.Equal(RecentEnd.AddHours(-5), finding.SinceUtc);
        Assert.Equal(42, finding.Evidence[AnomalyEvidenceNames.BaselineValue]);
        Assert.Equal(9.2, finding.Evidence[AnomalyEvidenceNames.DeltaFromBaseline]);
        Assert.Equal(22, finding.Evidence[AnomalyEvidenceNames.IdleLoadDeltaPoints]);
        Assert.Equal(TimeSpan.FromHours(5).TotalSeconds, finding.Evidence[AnomalyEvidenceNames.DurationSeconds]);

        // 差がしきい値の 2 倍以上（負荷 22 ≥ 20 ポイント）なら「警告」。
        Assert.Equal(AnomalySeverity.Warning, finding.Severity);
        Assert.Equal(AnomalySeverity.Notice, UsualFindingFactory.Create(part, new UsualComparisonResult(true, temperature, null), 8, Options)!.Severity);
        Assert.Null(UsualFindingFactory.Create(part, new UsualComparisonResult(true, null, null), 8, Options));
    }

    [Fact]
    public void PartsAreChosenFromTheSensorsThatExist()
    {
        var parts = UsualPartDefinitions.From([
            CpuLoad,
            SensorKey.Parse("gpu/abc/load/max-engine"),
            SensorKey.Parse("gpu/abc/temperature/adapter"),
            SensorKey.Parse("gpu/def/load/max-engine"),
        ]);

        Assert.Collection(
            parts,
            cpu =>
            {
                Assert.Equal(HardwareKind.Cpu, cpu.HardwareKind);
                Assert.Null(cpu.TemperatureKey);
                Assert.True(cpu.CompareIdleLoad);
            },
            gpu =>
            {
                Assert.Equal("gpu/abc", gpu.HardwareKey);
                Assert.Equal(SensorKey.Parse("gpu/abc/temperature/adapter"), gpu.TemperatureKey);
                Assert.False(gpu.CompareIdleLoad);
            });
    }

    [Fact]
    public void TheRuleJudgesHourlyAndTheSameAnomalyStaysOneRecordAcrossARestart()
    {
        var source = new FakeSource(History(Now - TimeSpan.FromDays(14), Now.AddHours(3), t => t >= RecentEnd.AddHours(-3) ? (2, 52) : Usual(t)));
        var clock = new ManualClock { Now = Now };
        var (detector, notices) = CreateDetector(new WorseThanUsualRule(source, run: Synchronously), clock);

        Tick(detector, clock, Now);
        Assert.Equal(0, source.Reads);

        // 起動して 2 分たったら最初の判定。3 時間続いているので異常が始まる（始まりは高くなった最初の区切り）。
        Tick(detector, clock, Now.AddMinutes(2));
        Assert.Equal(1, source.Reads);
        var started = Assert.Single(notices);
        Assert.Equal(AnomalyChange.Started, started.Change);
        Assert.True(started.ShouldNotify);
        Assert.Equal(RecentEnd.AddHours(-3), started.Anomaly.StartedUtc);

        // 次の判定は、次の時間の区切りの 2 分後（13:02）まで行わない。その間も同じ異常が続く。
        Tick(detector, clock, Now.AddMinutes(20));
        Assert.Equal(1, source.Reads);
        Tick(detector, clock, RecentEnd.AddHours(1).AddMinutes(2));
        Assert.Equal(2, source.Reads);
        Assert.Single(notices);

        _repository.MarkNotified(started.Anomaly.AnomalyId, Now.AddMinutes(3));

        // Helper を起動し直すと、続いていた異常はいったん終わるが、同じ状態の続きなので開き直す（1 件のまま、通知もしない）。
        var (restarted, restartNotices) = CreateDetector(new WorseThanUsualRule(source, run: Synchronously), clock);
        restarted.Start();
        Tick(restarted, clock, RecentEnd.AddHours(1).AddMinutes(10));
        Tick(restarted, clock, RecentEnd.AddHours(1).AddMinutes(12));

        Assert.Empty(restartNotices);
        var all = _repository.GetOverlapping(Now.AddDays(-1), Now.AddDays(1), includeDismissed: true, limit: 10);
        var record = Assert.Single(all);
        Assert.Equal(started.Anomaly.AnomalyId, record.AnomalyId);
        Assert.True(record.IsOngoing);
        Assert.NotNull(record.NotifiedUtc);
    }

    [Fact]
    public void ADismissedAnomalyStaysDismissedWhileItContinues()
    {
        var source = new FakeSource(History(Now - TimeSpan.FromDays(14), Now.AddHours(3), t => t >= RecentEnd.AddHours(-3) ? (2, 52) : Usual(t)));
        var clock = new ManualClock { Now = Now };
        var (detector, notices) = CreateDetector(new WorseThanUsualRule(source, run: Synchronously), clock);
        Tick(detector, clock, Now);
        Tick(detector, clock, Now.AddMinutes(2));
        var anomaly = Assert.Single(notices).Anomaly;

        // 「これは正常」と消すと、続いている間（次の判定でも、起動し直しても）は消したまま。
        var dismissed = _repository.SetDismissed(anomaly.AnomalyId, Now.AddMinutes(5))!;
        detector.OnDismissalChanged(dismissed);
        Tick(detector, clock, RecentEnd.AddHours(1).AddMinutes(2));

        var (restarted, restartNotices) = CreateDetector(new WorseThanUsualRule(source, run: Synchronously), clock);
        restarted.Start();
        Tick(restarted, clock, RecentEnd.AddHours(1).AddMinutes(10));
        Tick(restarted, clock, RecentEnd.AddHours(1).AddMinutes(12));

        Assert.Empty(restartNotices);
        Assert.True(_repository.Find(anomaly.AnomalyId)!.IsDismissed);
        Assert.Empty(_repository.GetOverlapping(Now.AddDays(-1), Now.AddDays(1), includeDismissed: false, limit: 10));
    }

    [Fact]
    public void TheHistorySourcePairsLoadAndTemperatureByMinute()
    {
        var start = RecentEnd;
        IReadOnlyList<SensorHistoryPoint> Read(SensorKey key, DateTimeOffset from, DateTimeOffset to, TimeSeriesResolution resolution)
        {
            Assert.Equal(TimeSeriesResolution.OneMinute, resolution);
            return key == CpuLoad
                ? [Point(start, 5), Point(start.AddMinutes(1), 6), Point(start.AddMinutes(2), 7), Point(start.AddMinutes(5), 8)]
                : [Point(start, 40), Point(start.AddMinutes(2), 41)];
        }

        var part = UsualPartDefinitions.From([CpuLoad, CpuTemperature]).Single();
        var minutes = new SensorHistoryUsualSource(Read).Read(part, start, start.AddHours(1));

        // 温度のない分は、2 分以内の直前の温度を使う。それより前なら温度なし。
        Assert.Equal(new double?[] { 40, 40, 41, null }, minutes.Select(m => m.TemperatureCelsius).ToArray());
        Assert.Equal(new[] { 5.0, 6, 7, 8 }, minutes.Select(m => m.LoadPercent).ToArray());
    }

    private (AnomalyDetector Detector, List<AnomalyNotice> Notices) CreateDetector(WorseThanUsualRule rule, ManualClock clock)
    {
        var settings = new AnomalySettingsCache(new SqliteAnomalySettingsRepository(_master));
        var detector = new AnomalyDetector([rule], _repository, settings, new AnomalyNotificationPolicy(), clock);
        var notices = new List<AnomalyNotice>();
        detector.AnomalyChanged += (_, n) => notices.Add(n);
        return (detector, notices);
    }

    private static void Tick(AnomalyDetector detector, ManualClock clock, DateTimeOffset at)
    {
        clock.Now = at;
        detector.OnReadings([SensorReading.Ok(CpuLoad, 2, at, "test"), SensorReading.Ok(CpuTemperature, 40, at, "test")]);
    }

    private static Task Synchronously(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    /// <summary>ふだん：偶数の分は負荷 2%・40℃、奇数の分は負荷 25%・55℃。</summary>
    private static (double Load, double? Temperature)? Usual(DateTimeOffset t) => t.Minute % 2 == 0 ? (2, 40) : (25, 55);

    private static List<UsualMinute> History(DateTimeOffset from, DateTimeOffset to, Func<DateTimeOffset, (double Load, double? Temperature)?> at)
    {
        var minutes = new List<UsualMinute>();
        for (var t = UsualStatistics.FloorTo(from, TimeSpan.FromMinutes(1)); t < to; t = t.AddMinutes(1))
        {
            if (at(t) is { } value)
            {
                minutes.Add(new UsualMinute(t, value.Load, value.Temperature));
            }
        }

        return minutes;
    }

    private static SensorHistoryPoint Point(DateTimeOffset at, double value) => new(at, value, value, value, 6);

    private sealed class FakeSource(IReadOnlyList<UsualMinute> minutes) : IUsualHistorySource
    {
        public int Reads { get; private set; }

        public IReadOnlyList<UsualMinute> Read(UsualPartDefinition part, DateTimeOffset fromUtc, DateTimeOffset toUtc)
        {
            Reads++;
            return minutes.Where(m => m.TimestampUtc >= fromUtc && m.TimestampUtc < toUtc).ToArray();
        }
    }
}

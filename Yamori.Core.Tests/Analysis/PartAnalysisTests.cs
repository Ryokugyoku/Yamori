using Yamori.Core.Analysis.Parts;
using Yamori.Core.Anomalies;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Analysis;

public class PartAnalysisTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly AnalysisHardware Cpu = new(AnalysisPart.Cpu, "0", "Intel Core i7");

    [Fact]
    public void FixedPeriodsEndNowAndCustomDatesCoverWholeLocalDays()
    {
        Assert.Equal(new AnalysisRange(Now.AddDays(-1), Now), AnalysisRanges.ForPeriod(AnalysisPeriod.Last24Hours, Now));
        Assert.Equal(new AnalysisRange(Now.AddDays(-90), Now), AnalysisRanges.ForPeriod(AnalysisPeriod.Last90Days, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnalysisRanges.ForPeriod(AnalysisPeriod.Custom, Now));

        // UTC+9 の 11 月 1 日〜3 日は、10 月 31 日 15 時（UTC）から 11 月 3 日 15 時（UTC）まで。日付が逆でも同じ。
        var tokyo = TimeZoneInfo.CreateCustomTimeZone("Test+9", TimeSpan.FromHours(9), "Test+9", "Test+9");
        var expected = new AnalysisRange(new DateTimeOffset(2026, 10, 31, 15, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 11, 3, 15, 0, 0, TimeSpan.Zero));
        Assert.Equal(expected, AnalysisRanges.ForDates(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 3), tokyo, Now));
        Assert.Equal(expected, AnalysisRanges.ForDates(new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 1), tokyo, Now));

        // 今より先と 90 日より前は切り詰める。すべて先なら直近 24 時間。
        var clipped = AnalysisRanges.ForDates(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), TimeZoneInfo.Utc, Now);
        Assert.Equal(new AnalysisRange(Now.AddDays(-90), Now), clipped);
        Assert.Equal(AnalysisRanges.ForPeriod(AnalysisPeriod.Last24Hours, Now), AnalysisRanges.ForDates(new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 2), TimeZoneInfo.Utc, Now));
    }

    [Fact]
    public void AnomalyIsCenteredWithPaddingAndAppUsageRangeIsWidenedToMinutes()
    {
        var start = Now.AddHours(-2);
        var anomaly = Anomaly(1, start, TimeSpan.FromMinutes(20));
        Assert.Equal(new AnalysisRange(start.AddMinutes(-20), start.AddMinutes(40)), AnalysisRanges.AroundAnomaly(anomaly, Now));

        // 短い異常は前後 5 分。続いている異常は今で切る。
        var shortOne = Anomaly(2, start, TimeSpan.FromSeconds(30));
        Assert.Equal(new AnalysisRange(start.AddMinutes(-5), start.AddSeconds(30).AddMinutes(5)), AnalysisRanges.AroundAnomaly(shortOne, Now));
        var ongoing = Anomaly(3, Now.AddMinutes(-1), TimeSpan.FromSeconds(50)) with { EndedUtc = null };
        var range = AnalysisRanges.AroundAnomaly(ongoing, Now);
        Assert.Equal(Now, range.ToUtc);
        Assert.True(range.Length >= AnalysisRanges.MinimumAnomalyWindow);

        var apps = AnalysisRanges.ForAppUsage(new AnalysisRange(start.AddSeconds(10), start.AddSeconds(40)));
        Assert.Equal(new AnalysisRange(start, start.AddMinutes(1)), apps);
        Assert.Equal(new AnalysisRange(start, start.AddMinutes(2)), AnalysisRanges.ForAppUsage(new AnalysisRange(start, start.AddMinutes(2))));
    }

    [Fact]
    public void UpTo24HoursUsesTheOneSecondRecordAndLongerRangesUseTheMinuteRollups()
    {
        Assert.Equal(TimeSeriesResolution.OneSecond, AnalysisResolutions.Choose(new AnalysisRange(Now.AddDays(-1), Now)));
        Assert.Equal(TimeSeriesResolution.OneMinute, AnalysisResolutions.Choose(new AnalysisRange(Now.AddDays(-1).AddSeconds(-1), Now)));
        Assert.Equal(TimeSeriesResolution.OneMinute, AnalysisResolutions.Fallback(TimeSeriesResolution.OneSecond));
        Assert.Null(AnalysisResolutions.Fallback(TimeSeriesResolution.OneMinute));
    }

    [Fact]
    public async Task ReaderSplitsLongRangesAndFallsBackToMinuteRollupsWhenThereIsNoFineRecord()
    {
        var key = SensorKey.Parse("storage/disk1/load/active-time");
        var sensors = new FakeSensorService { HistoryResolution = TimeSeriesResolution.OneMinute };
        sensors.History.Add(new SensorHistoryPoint(Now.AddMinutes(-30), 1, 9, 5, 60));
        var reader = new AnalysisHistoryReader(sensors);

        var pieces = await reader.ReadAsync(key, new AnalysisRange(Now.AddHours(-5), Now));

        // 1 秒ごとの記録を 2 時間ずつ 3 回読み、空だったので 1 分集計を 1 回で読み直す。
        var piece = Assert.Single(pieces);
        Assert.Equal(TimeSeriesResolution.OneMinute, piece.Resolution);
        Assert.Single(piece.Points);
        Assert.Equal(3, sensors.HistoryRequests.Count(r => r.Resolution == TimeSeriesResolution.OneSecond));
        Assert.Single(sensors.HistoryRequests, r => r.Resolution == TimeSeriesResolution.OneMinute);

        sensors.HistoryRequests.Clear();
        await reader.ReadAsync(key, new AnalysisRange(Now.AddDays(-20), Now));
        Assert.Equal(3, sensors.HistoryRequests.Count);
        Assert.All(sensors.HistoryRequests, r => Assert.Equal(TimeSeriesResolution.OneMinute, r.Resolution));
    }

    [Fact]
    public async Task ReaderFillsTheTimeBeforeTheFirstFineRecordWithMinuteRollups()
    {
        var key = SensorKey.Parse("cpu/0/temperature/package");
        var sensors = new FakeSensorService();
        sensors.HistoryByResolution[TimeSeriesResolution.OneSecond] = [new SensorHistoryPoint(Now.AddMinutes(-30), 50, 50, 50, 1)];
        sensors.HistoryByResolution[TimeSeriesResolution.OneMinute] =
            [new SensorHistoryPoint(Now.AddHours(-10), 40, 45, 42, 6), new SensorHistoryPoint(Now.AddMinutes(-31), 40, 45, 42, 6), new SensorHistoryPoint(Now.AddMinutes(-30), 1, 1, 1, 6)];

        var pieces = await new AnalysisHistoryReader(sensors).ReadAsync(key, new AnalysisRange(Now.AddDays(-1), Now));

        // 1 秒ごとの記録の前（30 分前まで）だけを 1 分集計で補う。重なる 1 分は使わない。
        Assert.Equal([TimeSeriesResolution.OneMinute, TimeSeriesResolution.OneSecond], pieces.Select(p => p.Resolution));
        Assert.Equal(2, pieces[0].Points.Count);
        Assert.Single(pieces[1].Points);
    }

    [Fact]
    public void StatisticsGiveMinAverageMaxPercentileAndTimeAboveTheThreshold()
    {
        var points = Enumerable.Range(1, 100)
            .Select(i => new SensorHistoryPoint(Now.AddSeconds(i), i, i, i, 1))
            .ToArray();

        var statistics = SeriesStatisticsCalculator.Calculate(points, TimeSpan.FromSeconds(1), threshold: 91);

        Assert.NotNull(statistics);
        Assert.Equal(1, statistics.Minimum);
        Assert.Equal(50.5, statistics.Average, 6);
        Assert.Equal(100, statistics.Maximum);
        Assert.Equal(95, statistics.Percentile95);
        Assert.Equal(TimeSpan.FromSeconds(100), statistics.Observed);
        Assert.Equal(TimeSpan.FromSeconds(10), statistics.AboveThreshold);

        // 最小・最大は集計の最小・最大から取る。
        var rollups = new[] { new SensorHistoryPoint(Now, 10, 90, 20, 50), new SensorHistoryPoint(Now.AddMinutes(1), 30, 40, 35, 10) };
        var minute = SeriesStatisticsCalculator.Calculate(rollups, TimeSpan.FromMinutes(1), null)!;
        Assert.Equal(10, minute.Minimum);
        Assert.Equal(90, minute.Maximum);
        Assert.Equal(27.5, minute.Average, 6);
        Assert.Null(minute.AboveThreshold);

        // 1 分集計（1 点 60 秒）と 1 秒ごとの記録がまざっても、時間の長さで重みをつける。
        var mixed = SeriesStatisticsCalculator.Calculate(
            [
                new HistoryPiece(TimeSeriesResolution.OneMinute, [new SensorHistoryPoint(Now, 90, 90, 90, 6)]),
                new HistoryPiece(TimeSeriesResolution.OneSecond, Enumerable.Range(0, 60).Select(i => new SensorHistoryPoint(Now.AddMinutes(1).AddSeconds(i), 30, 30, 30, 1)).ToArray()),
            ],
            threshold: 80)!;
        Assert.Equal(60, mixed.Average, 6);
        Assert.Equal(TimeSpan.FromMinutes(2), mixed.Observed);
        Assert.Equal(TimeSpan.FromMinutes(1), mixed.AboveThreshold);
        Assert.Equal(90, mixed.Percentile95);

        Assert.Null(SeriesStatisticsCalculator.Calculate([new SensorHistoryPoint(Now, double.NaN, double.NaN, double.NaN, 1)], TimeSpan.FromSeconds(1), null));
    }

    [Fact]
    public void DownsamplingKeepsShortPeaks()
    {
        var range = new AnalysisRange(Now, Now.AddSeconds(1000));
        var points = Enumerable.Range(0, 1000)
            .Select(i => new SensorHistoryPoint(Now.AddSeconds(i), i == 503 ? 99 : 10, i == 503 ? 99 : 10, i == 503 ? 99 : 10, 1))
            .ToArray();

        var buckets = SeriesDownsampler.Downsample(points, range, 100);

        Assert.Equal(100, buckets.Count);
        Assert.Equal(99, buckets.Max(b => b.Max));
        Assert.Equal(10, buckets.Min(b => b.Min));
        Assert.Equal(10u, buckets[0].Count);
        Assert.Same(points, SeriesDownsampler.Downsample(points, range, 2000));
    }

    [Fact]
    public void SeriesUseTheCatalogAndPreferTheEffectiveClock()
    {
        var catalog = new[]
        {
            SensorKey.Parse("cpu/0/load/total"),
            SensorKey.Parse("cpu/0/temperature/package"),
            SensorKey.Parse("cpu/0/clock/average-estimated"),
        };

        var series = AnalysisSeriesDefinitions.Resolve(Cpu, catalog);
        Assert.Equal([AnalysisSeriesKind.Load, AnalysisSeriesKind.Temperature, AnalysisSeriesKind.Clock], series.Select(s => s.Kind));
        Assert.Equal("cpu/0/clock/average-estimated", series[2].Key.ToString());

        var withEffective = AnalysisSeriesDefinitions.Resolve(Cpu, [.. catalog, SensorKey.Parse("cpu/0/clock/average-effective")]);
        Assert.Equal("cpu/0/clock/average-effective", withEffective[2].Key.ToString());

        // カタログにその部品がなければ（Helper につながっていないなど）最初の候補を使う。
        var unknown = AnalysisSeriesDefinitions.Resolve(new AnalysisHardware(AnalysisPart.Gpu, "abc", "GPU"), catalog);
        Assert.Equal(["gpu/abc/load/max-engine", "gpu/abc/temperature/core", "gpu/abc/clock/core"], unknown.Select(s => s.Key.ToString()));
        Assert.Single(AnalysisSeriesDefinitions.Resolve(new AnalysisHardware(AnalysisPart.Memory, "0", "Memory"), []));
    }

    [Fact]
    public void HardwareComesFromTheCatalogAndFromAnomaliesOfRemovedParts()
    {
        var catalog = new[]
        {
            Descriptor("storage/disk10/load/active-time", "Disk 10"),
            Descriptor("storage/disk2/load/active-time", "Disk 2"),
            Descriptor("cpu/0/load/total", "Intel Core i7"),
            Descriptor("battery/0/level/charge", "Battery"),
        };
        var removed = Anomaly(1, Now, TimeSpan.FromMinutes(1)) with { HardwareKey = "gpu/old", HardwareKind = HardwareKind.Gpu, HardwareName = "Old GPU" };

        var hardware = AnalysisHardwareCatalog.Build(catalog, [removed]);

        Assert.Equal(["Disk 2", "Disk 10"], hardware[AnalysisPart.Storage].Select(h => h.Name));
        Assert.Equal("gpu/old", hardware[AnalysisPart.Gpu].Single().HardwareKey);
        Assert.Equal("cpu/0", hardware[AnalysisPart.Cpu].Single().HardwareKey);
        Assert.False(hardware.ContainsKey(AnalysisPart.Network));
    }

    [Fact]
    public void AnomaliesAreNewestFirstAndCountedPerPartWithoutDismissedOnes()
    {
        var older = Anomaly(1, Now.AddHours(-3), TimeSpan.FromMinutes(1));
        var newer = Anomaly(2, Now.AddHours(-1), TimeSpan.FromMinutes(1));
        var gpu = Anomaly(3, Now.AddHours(-2), TimeSpan.FromMinutes(1)) with { HardwareKey = "gpu/a", HardwareKind = HardwareKind.Gpu };
        var dismissed = Anomaly(4, Now.AddHours(-4), TimeSpan.FromMinutes(1)) with { DismissedUtc = Now };
        var zone = Anomaly(5, Now.AddHours(-5), TimeSpan.FromMinutes(1)) with { HardwareKey = "thermal-zone/tz0", HardwareKind = HardwareKind.ThermalZone, Kind = AnomalyKind.WorseThanUsual };
        var all = new[] { older, newer, gpu, dismissed, zone };

        Assert.Equal([2L, 3, 1, 4, 5], AnomalyListBuilder.Select(all, null).Select(a => a.AnomalyId));
        Assert.Equal([2L, 1, 4], AnomalyListBuilder.Select(all, AnalysisPart.Cpu).Select(a => a.AnomalyId));
        Assert.Equal([3L], AnomalyListBuilder.Select(all, AnalysisPart.Gpu, "gpu/a").Select(a => a.AnomalyId));

        var counts = AnomalyListBuilder.CountByPart(all);
        Assert.Equal(2, counts[AnalysisPart.Cpu]);
        Assert.Equal(1, counts[AnalysisPart.Gpu]);
        Assert.Equal(1, counts[AnalysisPart.ThermalZone]);

        var bands = AnomalyListBuilder.Bands(all, "cpu/0", new AnalysisRange(Now.AddHours(-2), Now));
        Assert.Equal([2L], bands.Select(a => a.AnomalyId));
    }

    [Fact]
    public async Task LoaderReadsTheSeriesAndUsesTheThrottleTargetForTheHighTemperatureTime()
    {
        var sensors = new FakeSensorService();
        foreach (var key in new[] { "cpu/0/load/total", "cpu/0/temperature/package", "cpu/0/clock/average-effective" })
        {
            sensors.Catalog.Add(Descriptor(key, "Intel Core i7"));
        }

        var target = SensorKey.Parse("cpu/0/temperature/throttle-target");
        sensors.Latest[target] = SensorReading.Ok(target, 100, Now, "pawnio");
        for (var i = 0; i < 60; i++)
        {
            sensors.History.Add(new SensorHistoryPoint(Now.AddSeconds(-60 + i), 0, 0, i < 50 ? 80 : 96, 1));
        }

        var analysis = await new PartAnalysisLoader(sensors).LoadAsync(
            Cpu,
            new AnalysisRange(Now.AddMinutes(-10), Now),
            sensors.Catalog.Select(d => d.Key).ToArray(),
            manualThresholdCelsius: null);

        Assert.Equal(95, analysis.TemperatureThresholdCelsius);
        Assert.Equal(3, analysis.Series.Count);
        var temperature = analysis.Find(AnalysisSeriesKind.Temperature)!;
        Assert.Equal([TimeSeriesResolution.OneSecond], temperature.Resolutions);
        Assert.Equal(TimeSpan.FromSeconds(10), temperature.Statistics!.AboveThreshold);
        Assert.Null(analysis.Find(AnalysisSeriesKind.Load)!.Statistics!.AboveThreshold);

        // 手動の注意の温度は CPU に効き、ストレージには効かない。
        Assert.Equal(80, AnalysisTemperatureThreshold.Resolve(Cpu, 100, 80));
        Assert.Equal(70, AnalysisTemperatureThreshold.Resolve(new AnalysisHardware(AnalysisPart.Storage, "d", "Disk"), null, 80));
    }

    [Fact]
    public void AxisScalesAreRoundAndIncludeTheThreshold()
    {
        Assert.Equal(new ChartAxisScale(0, 100, 25), ChartAxisScales.For(AnalysisSeriesKind.Load, [], null));

        var temperature = ChartAxisScales.For(AnalysisSeriesKind.Temperature, [new SensorHistoryPoint(Now, 41, 78, 60, 1)], 95);
        Assert.True(temperature.Minimum <= 36 && temperature.Maximum >= 100);
        Assert.Equal(0, temperature.Minimum % temperature.Step, 6);

        var clock = ChartAxisScales.For(AnalysisSeriesKind.Clock, [new SensorHistoryPoint(Now, 0.8e9, 4.7e9, 3e9, 1)], null);
        Assert.Equal(0, clock.Minimum);
        Assert.Equal(6e9, clock.Maximum);
        Assert.Equal([0, 2e9, 4e9, 6e9], clock.Ticks());
    }

    [Fact]
    public void TimeTicksAreRoundLocalTimes()
    {
        var tokyo = TimeZoneInfo.CreateCustomTimeZone("Test+9", TimeSpan.FromHours(9), "Test+9", "Test+9");
        var range = new AnalysisRange(new DateTimeOffset(2026, 11, 8, 3, 7, 0, TimeSpan.Zero), new DateTimeOffset(2026, 11, 8, 3, 52, 0, TimeSpan.Zero));

        var ticks = TimeAxisTicks.Create(range, tokyo, 6);

        Assert.Equal(TimeSpan.FromMinutes(10), TimeAxisTicks.ChooseStep(range, 6));
        Assert.Equal([10, 20, 30, 40, 50], ticks.Select(t => t.ToOffset(TimeSpan.FromHours(9)).Minute));

        var week = TimeAxisTicks.Create(new AnalysisRange(Now.AddDays(-7), Now), tokyo, 8);
        Assert.Equal(7, week.Count);
        Assert.All(week, t => Assert.Equal(0, t.ToOffset(TimeSpan.FromHours(9)).Hour));
    }

    private static SensorDescriptor Descriptor(string key, string hardwareName) =>
        new(SensorKey.Parse(key), SensorUnit.Percent, key, hardwareName, "load", "test");

    private static AnomalyRecord Anomaly(long id, DateTimeOffset start, TimeSpan duration) =>
        new(id, AnomalyKind.ThermalThrottling, "cpu/0", HardwareKind.Cpu, "Intel Core i7", start, start + duration, start + duration,
            AnomalySeverity.Warning, new Dictionary<string, double>(), null, null);
}

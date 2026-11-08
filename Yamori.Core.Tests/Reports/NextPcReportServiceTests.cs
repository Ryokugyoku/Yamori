using Yamori.Core.Analysis.History;
using Yamori.Core.Hardware;
using Yamori.Core.Monitoring;
using Yamori.Core.Processes;
using Yamori.Core.Recommendations;
using Yamori.Core.Reports;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Reports;

public class NextPcReportServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedInventoryProvider(HardwareInventory inventory) : IHardwareInventoryProvider
    {
        public HardwareInventory GetInventory() => inventory;
    }

    private sealed class FixedProcessProvider(params ProcessSnapshot[] snapshots) : IProcessSnapshotProvider
    {
        public IReadOnlyList<ProcessSnapshot> Capture() => snapshots;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static NextPcReportService CreateService(IUsageHistoryReader reader) =>
        new(
            new FixedInventoryProvider(HardwareInventory.Empty with { Memory = new MemoryInfo(8UL << 30, 1, null) }),
            reader,
            new FixedProcessProvider(new ProcessSnapshot(1, "chrome", 3UL << 30)),
            NextPcAdvisor.CreateDefault(),
            new FixedTimeProvider(Now));

    [Fact]
    public async Task BuildsReportFromHistoryWithinLookback()
    {
        var store = new InMemoryUsageHistoryStore();

        // 直近 2 時間はずっとメモリ 95%。それより前の古いデータは集計期間外。
        store.Samples.Add(new ComponentUsageSample(HardwareComponent.Memory, Now.AddDays(-200), 10));
        for (var minute = 120; minute >= 0; minute--)
        {
            store.Samples.Add(new ComponentUsageSample(HardwareComponent.Memory, Now.AddMinutes(-minute), 95));
        }

        var report = await CreateService(new CsvUsageHistoryReader(store)).CreateAsync(TimeSpan.FromDays(90));

        var memory = report.Recommendations.Single(r => r.Component == HardwareComponent.Memory);
        Assert.Equal(RecommendationLevel.Insufficient, memory.Level);
        Assert.Equal(16, memory.RecommendedAmount);
        Assert.Equal(TimeSpan.FromHours(2), memory.HighLoad.TotalObserved);
        Assert.Equal("chrome", Assert.Single(report.TopApps).Name);
        Assert.Equal(Now, report.GeneratedAt);
        Assert.Equal(UsageHistoryOrigin.AppCsv, report.OriginOf(HardwareComponent.Memory));
        Assert.Equal(UsageHistoryOrigin.None, report.OriginOf(HardwareComponent.Cpu));
    }

    [Fact]
    public async Task PrefersHelperTimeSeriesOverAppCsv()
    {
        // Helper の 1 分集計では 2 時間ずっとメモリ 95%。画面の CSV では余裕がある（使わないはず）。
        var sensors = new FakeSensorService();
        for (var minute = 120; minute >= 0; minute--)
        {
            sensors.History.Add(new SensorHistoryPoint(Now.AddMinutes(-minute - 1), 90, 99, 95, 6));
        }

        var store = new InMemoryUsageHistoryStore();
        for (var minute = 120; minute >= 0; minute--)
        {
            store.Samples.Add(new ComponentUsageSample(HardwareComponent.Memory, Now.AddMinutes(-minute), 10));
        }

        var reader = new FallbackUsageHistoryReader(new SensorUsageHistoryReader(sensors), new CsvUsageHistoryReader(store));
        var report = await CreateService(reader).CreateAsync(TimeSpan.FromDays(90));

        var memory = report.Recommendations.Single(r => r.Component == HardwareComponent.Memory);
        Assert.Equal(RecommendationLevel.Insufficient, memory.Level);
        Assert.Equal(TimeSpan.FromHours(2), memory.HighLoad.TotalObserved);
        Assert.Equal(UsageHistoryOrigin.HelperTimeSeries, report.OriginOf(HardwareComponent.Memory));
    }
}

using Yamori.Core.Hardware;
using Yamori.Core.Monitoring;
using Yamori.Core.Processes;
using Yamori.Core.Recommendations;
using Yamori.Core.Reports;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Reports;

public class NextPcReportServiceTests
{
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

    [Fact]
    public void BuildsReportFromHistoryWithinLookback()
    {
        var now = new DateTimeOffset(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);
        var store = new InMemoryUsageHistoryStore();

        // 直近 2 時間はずっとメモリ 95%。それより前の古いデータは集計期間外。
        store.Samples.Add(new ComponentUsageSample(HardwareComponent.Memory, now.AddDays(-200), 10));
        for (var minute = 120; minute >= 0; minute--)
        {
            store.Samples.Add(new ComponentUsageSample(HardwareComponent.Memory, now.AddMinutes(-minute), 95));
        }

        var inventory = HardwareInventory.Empty with { Memory = new MemoryInfo(8UL << 30, 1, null) };
        var service = new NextPcReportService(
            new FixedInventoryProvider(inventory),
            store,
            new FixedProcessProvider(new ProcessSnapshot(1, "chrome", 3UL << 30)),
            NextPcAdvisor.CreateDefault(),
            new FixedTimeProvider(now));

        var report = service.Create(TimeSpan.FromDays(90));

        var memory = report.Recommendations.Single(r => r.Component == HardwareComponent.Memory);
        Assert.Equal(RecommendationLevel.Insufficient, memory.Level);
        Assert.Equal(16, memory.RecommendedAmount);
        Assert.Equal(TimeSpan.FromHours(2), memory.HighLoad.TotalObserved);
        Assert.Equal("chrome", Assert.Single(report.TopApps).Name);
        Assert.Equal(now, report.GeneratedAt);
    }
}

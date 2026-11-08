using Yamori.Core.Analysis.History;
using Yamori.Core.AppUsage.Analysis;
using Yamori.Core.Hardware;
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

    private static NextPcReportService CreateService(FakeSensorService sensors, FakeAppUsageService? appUsage = null) =>
        new(
            new FixedInventoryProvider(HardwareInventory.Empty with
            {
                Memory = new MemoryInfo(8UL << 30, 1, null),
                Cpu = new CpuInfo("Test CPU", 4, 8, 3000),
            }),
            new SensorUsageHistoryReader(sensors),
            new FixedProcessProvider(new ProcessSnapshot(1, "chrome", 3UL << 30)),
            appUsage ?? new FakeAppUsageService(),
            NextPcAdvisor.CreateDefault(),
            new FixedTimeProvider(Now));

    [Fact]
    public async Task BuildsReportFromHelperHistoryWithinLookback()
    {
        // 直近 2 時間はずっとメモリ 95%（1 分集計）。それより前の古いデータは集計期間外。
        var sensors = new FakeSensorService();
        sensors.History.Add(new SensorHistoryPoint(Now.AddDays(-200), 10, 10, 10, 6));
        for (var minute = 121; minute >= 1; minute--)
        {
            sensors.History.Add(new SensorHistoryPoint(Now.AddMinutes(-minute), 90, 99, 95, 6));
        }

        var report = await CreateService(sensors).CreateAsync(TimeSpan.FromDays(90));

        var memory = report.Recommendations.Single(r => r.Component == HardwareComponent.Memory);
        Assert.Equal(RecommendationLevel.Insufficient, memory.Level);
        Assert.Equal(16, memory.RecommendedAmount);
        Assert.Equal(TimeSpan.FromHours(2), memory.HighLoad.TotalObserved);
        Assert.Equal("chrome", Assert.Single(report.TopApps).Name);
        Assert.Equal(Now, report.GeneratedAt);
        Assert.Equal(UsageHistoryOrigin.HelperTimeSeries, report.OriginOf(HardwareComponent.Memory));
    }

    [Fact]
    public async Task ReportsNoOriginWithoutHelperHistory()
    {
        var report = await CreateService(new FakeSensorService()).CreateAsync();

        var memory = report.Recommendations.Single(r => r.Component == HardwareComponent.Memory);
        Assert.Equal(TimeSpan.Zero, memory.HighLoad.TotalObserved);
        Assert.Equal(UsageHistoryOrigin.None, report.OriginOf(HardwareComponent.Memory));
    }

    [Fact]
    public async Task ShowsRecordedHighLoadAppsAsCpuCauses()
    {
        // 直近 2 時間はずっと CPU 95%。高負荷の時間に多く使っていたのは render と chrome。
        var sensors = new FakeSensorService();
        for (var minute = 121; minute >= 1; minute--)
        {
            sensors.History.Add(new SensorHistoryPoint(Now.AddMinutes(-minute), 90, 99, 95, 6));
        }

        var appUsage = new FakeAppUsageService
        {
            Summary = new AppCpuUsageSummary(TimeSpan.FromHours(2), TimeSpan.FromHours(2), 80, [
                new AppCpuUsage("chrome", 30, 30),
                new AppCpuUsage("render", 20, 50),
                new AppCpuUsage("code", 10, 10),
            ]),
        };

        var report = await CreateService(sensors, appUsage).CreateAsync(TimeSpan.FromDays(90));

        var cpu = report.Recommendations.Single(r => r.Component == HardwareComponent.Cpu);
        Assert.Equal(RecommendationLevel.Insufficient, cpu.Level);
        Assert.Equal(["render", "chrome"], cpu.MainCauses);
        Assert.Equal((Now.AddDays(-90), Now, 80d, NextPcReportService.TopAppCount), appUsage.LastRequest);
    }

    [Fact]
    public async Task ShowsNoCpuCausesWithoutAppRecords()
    {
        var sensors = new FakeSensorService();
        for (var minute = 121; minute >= 1; minute--)
        {
            sensors.History.Add(new SensorHistoryPoint(Now.AddMinutes(-minute), 90, 99, 95, 6));
        }

        var report = await CreateService(sensors).CreateAsync(TimeSpan.FromDays(90));

        Assert.Empty(report.Recommendations.Single(r => r.Component == HardwareComponent.Cpu).MainCauses);
    }
}
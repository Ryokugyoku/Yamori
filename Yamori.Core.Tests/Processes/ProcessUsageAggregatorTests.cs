using Yamori.Core.Processes;

namespace Yamori.Core.Tests.Processes;

public class ProcessUsageAggregatorTests
{
    [Fact]
    public void GroupsByNameAndSortsByMemory()
    {
        ProcessSnapshot[] snapshots =
        [
            new(1, "chrome", 300),
            new(2, "Chrome", 400),
            new(3, "Teams", 500),
            new(4, "notepad", 10),
            new(5, "Memory Compression", 10_000),
        ];

        var top = ProcessUsageAggregator.TopByMemory(snapshots, top: 2);

        Assert.Equal(2, top.Count);
        Assert.Equal(("chrome", 2, 700UL), (top[0].Name, top[0].ProcessCount, top[0].WorkingSetBytes));
        Assert.Equal("Teams", top[1].Name);
    }

    [Fact]
    public void GroupsActivityByNameLikeMemoryAggregation()
    {
        ProcessActivity[] activities =
        [
            new(1, "chrome", 10, 20, 100, 0),
            new(2, "Chrome", 15, 90, 0, 50),
            new(3, "System", 50, 0, 1_000_000, 0),
            new(4, "notepad", 0, 0, 0, 0),
        ];

        var apps = ProcessActivityAggregator.GroupByApp(activities);

        Assert.Equal(2, apps.Count);
        var chrome = apps.Single(a => a.Name == "chrome");
        Assert.Equal((2, 25.0, 100.0, 150.0), (chrome.ProcessCount, chrome.CpuPercent, chrome.GpuPercent, chrome.IoBytesPerSecond));
        Assert.Empty(ProcessActivityAggregator.Top(apps.Where(a => a.Name == "notepad"), a => a.CpuPercent, 5));
    }

    [Fact]
    public void TopActivityOrdersByValueThenName()
    {
        AppActivity[] apps = [new("b", 1, 10, 0, 0, 0), new("a", 1, 10, 0, 0, 0), new("c", 1, 30, 0, 0, 0)];

        Assert.Equal(["c", "a"], ProcessActivityAggregator.Top(apps, a => a.CpuPercent, 2).Select(a => a.Name));
    }

    [Fact]
    public void RejectsNonPositiveTop()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProcessUsageAggregator.TopByMemory([], 0));
    }
}

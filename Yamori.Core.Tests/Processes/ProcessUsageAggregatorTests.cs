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
    public void RejectsNonPositiveTop()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProcessUsageAggregator.TopByMemory([], 0));
    }
}

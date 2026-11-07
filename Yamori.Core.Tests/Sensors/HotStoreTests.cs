using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Hot;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Sensors;

public class HotStoreTests
{
    private static readonly SensorKey CpuLoad = SensorKey.Parse("cpu/0/load/total");
    private static readonly SensorKey MemoryLoad = SensorKey.Parse("memory/0/load/physical");
    private static readonly DateTimeOffset Start = new(2026, 11, 7, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RingBufferOverwritesOldestWhenFull()
    {
        var buffer = new SensorRingBuffer(3);
        for (var i = 0; i < 5; i++)
        {
            buffer.Add(Reading(CpuLoad, i, i));
        }

        Assert.Equal(3, buffer.Count);
        Assert.Equal([2.0, 3.0, 4.0], buffer.Since(DateTimeOffset.MinValue).Select(r => r.Value!.Value));
        Assert.Equal(4, buffer.Latest()!.Value);
    }

    [Fact]
    public void RingBufferReturnsOnlyReadingsSinceTheGivenTime()
    {
        var buffer = new SensorRingBuffer(10);
        for (var i = 0; i < 5; i++)
        {
            buffer.Add(Reading(CpuLoad, i, i));
        }

        Assert.Equal([3.0, 4.0], buffer.Since(Start.AddSeconds(3)).Select(r => r.Value!.Value));
    }

    [Fact]
    public void EmptyRingBufferHasNoLatest()
    {
        var buffer = new SensorRingBuffer(2);

        Assert.Null(buffer.Latest());
        Assert.Empty(buffer.Since(DateTimeOffset.MinValue));
    }

    [Fact]
    public void StoreKeepsLatestPerSensorAndRaisesAppended()
    {
        var store = new InMemoryHotStore(new ManualClock { Now = Start.AddSeconds(10) }, capacity: 5);
        IReadOnlyList<SensorReading>? appended = null;
        store.Appended += (_, readings) => appended = readings;

        store.Append([Reading(CpuLoad, 0, 10), Reading(MemoryLoad, 0, 40)]);
        store.Append([Reading(CpuLoad, 1, 20)]);

        Assert.Equal(20, store.GetLatest(CpuLoad)!.Value);
        Assert.Equal(40, store.GetLatest(MemoryLoad)!.Value);
        Assert.Null(store.GetLatest(SensorKey.Parse("gpu/abc/load/max-engine")));
        Assert.NotNull(appended);
        Assert.Single(appended);
    }

    [Fact]
    public void StoreReturnsRecentWindowRelativeToNow()
    {
        var clock = new ManualClock { Now = Start.AddSeconds(4) };
        var store = new InMemoryHotStore(clock);
        for (var i = 0; i < 5; i++)
        {
            store.Append([Reading(CpuLoad, i, i)]);
        }

        Assert.Equal([2.0, 3.0, 4.0], store.GetRecent(CpuLoad, TimeSpan.FromSeconds(2)).Select(r => r.Value!.Value));
        Assert.Empty(store.GetRecent(MemoryLoad, TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void StoreKeepsUnavailableReadingsSoGraphsShowGaps()
    {
        var store = new InMemoryHotStore(new ManualClock { Now = Start.AddSeconds(1) });
        store.Append([Reading(CpuLoad, 0, 10)]);
        store.Append([SensorReading.Failed(CpuLoad, SensorStatus.TemporarilyUnavailable, Start.AddSeconds(1), "win-std")]);

        Assert.Equal(SensorStatus.TemporarilyUnavailable, store.GetLatest(CpuLoad)!.Status);
        Assert.Equal(2, store.GetRecent(CpuLoad, TimeSpan.FromMinutes(1)).Count);
    }

    [Fact]
    public void RetainOnlyDropsRemovedSensors()
    {
        var store = new InMemoryHotStore(new ManualClock());
        store.Append([Reading(CpuLoad, 0, 10), Reading(MemoryLoad, 0, 40)]);

        store.RetainOnly(new HashSet<SensorKey> { CpuLoad });

        Assert.NotNull(store.GetLatest(CpuLoad));
        Assert.Null(store.GetLatest(MemoryLoad));
    }

    private static SensorReading Reading(SensorKey key, int second, double value) =>
        SensorReading.Ok(key, value, Start.AddSeconds(second), "win-std");
}

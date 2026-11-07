using Yamori.Core.Sensors;
using Yamori.Core.Sensors.TimeSeries;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Sensors;

public class RecorderTests
{
    private static readonly SensorKey CpuLoad = SensorKey.Parse("cpu/0/load/total");
    private static readonly DateTimeOffset Start = new(2026, 11, 7, 0, 0, 0, TimeSpan.Zero);
    private static readonly SensorLogPolicy EveryTenSeconds = new(true, TimeSpan.FromSeconds(10), 1.0);

    [Fact]
    public void FilterHonoursIntervalAndDeadband()
    {
        var filter = new LogDecisionFilter(TimeSpan.FromMinutes(1));

        Assert.True(filter.ShouldRecord(At(0, 50), EveryTenSeconds));
        Assert.False(filter.ShouldRecord(At(5, 90), EveryTenSeconds));   // 間隔より短い
        Assert.False(filter.ShouldRecord(At(10, 50.5), EveryTenSeconds)); // デッドバンド未満
        Assert.True(filter.ShouldRecord(At(20, 52), EveryTenSeconds));   // 差が 1 以上
    }

    [Fact]
    public void FilterRecordsAtHeartbeatEvenWithoutChange()
    {
        var filter = new LogDecisionFilter(TimeSpan.FromMinutes(1));
        filter.ShouldRecord(At(0, 50), EveryTenSeconds);

        Assert.False(filter.ShouldRecord(At(30, 50), EveryTenSeconds));
        Assert.True(filter.ShouldRecord(At(60, 50), EveryTenSeconds));
    }

    [Fact]
    public void FilterSkipsDisabledAndUnreadableValues()
    {
        var filter = new LogDecisionFilter();

        Assert.False(filter.ShouldRecord(At(0, 50), SensorLogPolicy.Disabled));
        Assert.False(filter.ShouldRecord(
            SensorReading.Failed(CpuLoad, SensorStatus.TemporarilyUnavailable, Start, "win-std"),
            EveryTenSeconds));
    }

    [Fact]
    public void RecorderBuffersUntilFlushAndMapsIds()
    {
        var store = new InMemoryTimeSeriesStore();
        var recorder = new TimeSeriesRecorder(store, new LogDecisionFilter());
        recorder.UpdateTargets(
            new Dictionary<SensorKey, SensorLogTarget> { [CpuLoad] = new(7, EveryTenSeconds) },
            new Dictionary<string, ushort> { ["win-std"] = 2 });

        recorder.Accept([At(0, 50), SensorReading.Ok(SensorKey.Parse("memory/0/load/physical"), 30, Start, "win-std")]);

        Assert.Empty(store.ReadRaw(7, 0, long.MaxValue));
        Assert.Equal(1, recorder.Flush());
        var point = Assert.Single(store.ReadRaw(7, 0, long.MaxValue));
        Assert.Equal(new TimeSeriesPoint(7, Start.ToUnixTimeMilliseconds(), 50, 2), point);
        Assert.Equal(0, recorder.Flush());
    }

    [Fact]
    public void RecorderKeepsPointsWhenTheWriteFails()
    {
        var store = new InMemoryTimeSeriesStore { ThrowOnWrite = true };
        var recorder = new TimeSeriesRecorder(store, new LogDecisionFilter());
        recorder.UpdateTargets(
            new Dictionary<SensorKey, SensorLogTarget> { [CpuLoad] = new(7, EveryTenSeconds) },
            new Dictionary<string, ushort> { ["win-std"] = 1 });
        recorder.Accept([At(0, 50)]);

        Assert.Throws<IOException>(() => recorder.Flush());
        Assert.Equal(1, recorder.PendingCount);

        store.ThrowOnWrite = false;
        Assert.Equal(1, recorder.Flush());
    }

    [Fact]
    public void DefaultPoliciesSkipFineGrainedSensors()
    {
        SensorDescriptor Descriptor(string key, string group) =>
            new(SensorKey.Parse(key), SensorUnit.Percent, "x", "hw", group, "win-std");

        Assert.True(DefaultSensorLogPolicies.For(Descriptor("cpu/0/load/total", "load")).Enabled);
        Assert.False(DefaultSensorLogPolicies.For(Descriptor("cpu/0/load/thread-3", "threads")).Enabled);
        Assert.False(DefaultSensorLogPolicies.For(Descriptor("gpu/abc/load/engine-3d", "engines")).Enabled);
        Assert.False(DefaultSensorLogPolicies.For(Descriptor("cpu/0/voltage/core-0", "cores")).Enabled);
        Assert.Equal(TimeSpan.FromSeconds(10), DefaultSensorLogPolicies.For(Descriptor("cpu/0/load/total", "load")).Interval);
    }

    private static SensorReading At(int second, double value) =>
        SensorReading.Ok(CpuLoad, value, Start.AddSeconds(second), "win-std");
}

using Yamori.Core.Analysis.History;
using Yamori.Core.Hardware;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Analysis.History;

public class UsageHistoryReaderTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReadsOneMinuteRollupsInChunks()
    {
        var sensors = new FakeSensorService();
        sensors.History.Add(new SensorHistoryPoint(Now.AddDays(-20), 10, 90, 55, 6));
        sensors.History.Add(new SensorHistoryPoint(Now.AddDays(-1), 0, 130, 120, 6));
        var reader = new SensorUsageHistoryReader(sensors);

        var history = await reader.ReadAsync(HardwareComponent.Cpu, Now.AddDays(-30), Now);

        Assert.Equal(UsageHistoryOrigin.HelperTimeSeries, history.Origin);
        Assert.Equal([55, 100], history.Samples.Select(s => s.UsagePercent));
        Assert.All(sensors.HistoryRequests, r =>
        {
            Assert.Equal(WellKnownSensorKeys.CpuTotalLoad, r.Key);
            Assert.Equal(TimeSeriesResolution.OneMinute, r.Resolution);
            Assert.True(r.To - r.From <= SensorUsageHistoryReader.ChunkLength);
        });
        Assert.Equal(5, sensors.HistoryRequests.Count);
        Assert.Equal(Now.AddDays(-30), sensors.HistoryRequests[0].From);
        Assert.Equal(Now, sensors.HistoryRequests[^1].To);
    }

    [Fact]
    public async Task SkipsEmptyAndNonFinitePoints()
    {
        var sensors = new FakeSensorService();
        sensors.History.Add(new SensorHistoryPoint(Now.AddHours(-2), 0, 0, 0, 0));
        sensors.History.Add(new SensorHistoryPoint(Now.AddHours(-1), double.NaN, double.NaN, double.NaN, 3));
        var reader = new SensorUsageHistoryReader(sensors);

        var history = await reader.ReadAsync(HardwareComponent.Memory, Now.AddDays(-1), Now);

        Assert.Same(UsageHistory.Empty, history);
        Assert.Equal(UsageHistoryOrigin.None, history.Origin);
    }

    [Fact]
    public async Task ReturnsEmptyForComponentsWithoutUsageSensor()
    {
        var sensors = new FakeSensorService();
        var reader = new SensorUsageHistoryReader(sensors);

        var history = await reader.ReadAsync(HardwareComponent.Gpu, Now.AddDays(-1), Now);

        Assert.True(history.IsEmpty);
        Assert.Empty(sensors.HistoryRequests);
    }
}

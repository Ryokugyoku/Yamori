using Yamori.Core.Hardware;
using Yamori.Core.Monitoring;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Monitoring;

public class UsageMonitorTests
{
    [Fact]
    public void SampleOnceStoresOnlyMeasuredValues()
    {
        var store = new InMemoryUsageHistoryStore();
        var monitor = new UsageMonitor(
            [new FixedUsageSensor(HardwareComponent.Cpu, null), new FixedUsageSensor(HardwareComponent.Memory, 42)],
            store,
            TimeProvider.System);

        IReadOnlyList<ComponentUsageSample>? raised = null;
        monitor.SamplesRecorded += (_, samples) => raised = samples;

        var recorded = monitor.SampleOnce();

        var sample = Assert.Single(recorded);
        Assert.Equal(HardwareComponent.Memory, sample.Component);
        Assert.Equal(42, sample.UsagePercent);
        Assert.Equal(recorded, store.Samples);
        Assert.Same(recorded, raised);
    }

    [Fact]
    public void RejectsNonPositiveInterval()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UsageMonitor([], new InMemoryUsageHistoryStore(), TimeProvider.System, TimeSpan.Zero));
    }
}

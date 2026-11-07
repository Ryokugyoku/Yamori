using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Collection;
using Yamori.Core.Sensors.Hot;
using Yamori.Core.Sensors.Registry;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Sensors;

public class SensorCollectionLoopTests
{
    private static readonly SensorKey CpuLoad = SensorKey.Parse("cpu/0/load/total");
    private static readonly SensorKey GpuLoad = SensorKey.Parse("gpu/abc/load/max-engine");

    [Fact]
    public async Task TickBeforeDiscoveryWritesNothing()
    {
        var (loop, _, hot, _, _) = Create();
        await loop.TickAsync(CancellationToken.None);

        Assert.Null(hot.GetLatest(CpuLoad));
    }

    [Fact]
    public async Task HardwareChangeTriggersRediscoveryAfterTheDelay()
    {
        var (loop, registry, hot, notifier, clock) = Create();
        await registry.DiscoverAsync(CancellationToken.None);
        await loop.TickAsync(CancellationToken.None);
        Assert.Equal(10, hot.GetLatest(CpuLoad)!.Value);

        // GPU を追加した。
        notifier.Provider.Declare(GpuLoad, 20);
        notifier.Raise();
        await loop.TickAsync(CancellationToken.None);
        Assert.Null(hot.GetLatest(GpuLoad));

        clock.Now += TimeSpan.FromSeconds(3);
        await loop.TickAsync(CancellationToken.None);
        Assert.Equal(20, hot.GetLatest(GpuLoad)!.Value);
    }

    [Fact]
    public async Task RunDiscoversFirstAndStopsOnCancellation()
    {
        var (loop, registry, _, _, _) = Create();
        using var cancellation = new CancellationTokenSource();
        registry.CatalogChanged += (_, _) => cancellation.Cancel();

        await loop.RunAsync(cancellation.Token);

        Assert.Single(registry.Catalog);
    }

    private static (SensorCollectionLoop Loop, SensorRegistry Registry, InMemoryHotStore Hot, Notifier Notifier, ManualClock Clock) Create()
    {
        var clock = new ManualClock();
        var provider = new FakeSensorProvider("win-std").Declare(CpuLoad, 10);
        var registry = new SensorRegistry([provider], new SensorPriorityPolicy(SensorPriorityOptions.Default), clock);
        var hot = new InMemoryHotStore(clock);
        var notifier = new Notifier(provider);
        var loop = new SensorCollectionLoop(registry, hot, notifier, clock, TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(3));
        return (loop, registry, hot, notifier, clock);
    }

    private sealed class Notifier(FakeSensorProvider provider) : IHardwareChangeNotifier
    {
        public event EventHandler? Changed;

        public FakeSensorProvider Provider { get; } = provider;

        public void Raise() => Changed?.Invoke(this, EventArgs.Empty);
    }
}

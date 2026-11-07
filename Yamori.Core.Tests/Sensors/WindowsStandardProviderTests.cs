using Yamori.Core.Realtime;
using Yamori.Core.Realtime.Gpu;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Providers.WindowsStandard;
using Yamori.Core.Sensors.Providers.WindowsStandard.Mappers;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Sensors;

public class WindowsStandardProviderTests
{
    private static readonly SensorKey CpuTotal = WellKnownSensorKeys.CpuTotalLoad;
    private static readonly SensorKey ContextSwitches = SensorKey.Parse("cpu/0/rate/context-switches");
    private static readonly SensorKey PackageTemperature = SensorKey.Parse("cpu/0/temperature/package");

    [Fact]
    public async Task DiscoverDeclaresSensorsThatProducedAValueInEitherProbe()
    {
        // 1 秒あたりの値は 2 回目の測定から分かる。
        var source = new FakeSnapshotSource(
            FakeSnapshotSource.Snapshot(cpu: new CpuRealtime { UsagePercent = 10 }),
            FakeSnapshotSource.Snapshot(cpu: new CpuRealtime { UsagePercent = 12, ContextSwitchesPerSecond = 5000 }));
        using var provider = CreateProvider(source);

        var catalog = await provider.DiscoverAsync(CancellationToken.None);

        Assert.Equal(2, source.TakeCount);
        Assert.Contains(catalog, d => d.Key == CpuTotal);
        Assert.Contains(catalog, d => d.Key == ContextSwitches);
        Assert.DoesNotContain(catalog, d => d.Key == SensorKey.Parse("cpu/0/clock/base"));
        Assert.All(catalog, d => Assert.Equal("win-std", d.ProviderId));
    }

    [Fact]
    public async Task ReadReportsOkUnavailableAndUnsupported()
    {
        var source = new FakeSnapshotSource(
            FakeSnapshotSource.Snapshot(cpu: new CpuRealtime { UsagePercent = 10, ContextSwitchesPerSecond = 5000 }),
            FakeSnapshotSource.Snapshot(cpu: new CpuRealtime { UsagePercent = 10, ContextSwitchesPerSecond = 5000 }),
            FakeSnapshotSource.Snapshot(cpu: new CpuRealtime { UsagePercent = 42 }));
        using var provider = CreateProvider(source);
        await provider.DiscoverAsync(CancellationToken.None);

        var readings = await provider.ReadAsync([CpuTotal, ContextSwitches, PackageTemperature], CancellationToken.None);

        Assert.Equal(SensorStatus.Ok, readings[0].Status);
        Assert.Equal(42, readings[0].Value);
        Assert.Equal("win-std", readings[0].SourceProviderId);
        // 宣言したが今回は値が出なかった。null から「提供していない」とは推測しない。
        Assert.Equal(SensorStatus.TemporarilyUnavailable, readings[1].Status);
        Assert.Equal(SensorStatus.Unsupported, readings[2].Status);
    }

    [Fact]
    public async Task ReadFailureReturnsErrorForDeclaredKeys()
    {
        var source = new FakeSnapshotSource(FakeSnapshotSource.Snapshot(cpu: new CpuRealtime { UsagePercent = 10 }));
        using var provider = CreateProvider(source);
        await provider.DiscoverAsync(CancellationToken.None);
        source.ThrowOnTake = true;

        var reading = Assert.Single(await provider.ReadAsync([CpuTotal], CancellationToken.None));

        Assert.Equal(SensorStatus.Error, reading.Status);
    }

    [Fact]
    public async Task ProviderDeclaresNothingWhenTheSourceCannotBeCreated()
    {
        using var provider = new WindowsStandardProvider(
            () => throw new InvalidOperationException("PDH is broken"),
            [new CpuSensorMapper()],
            new ManualClock(),
            TimeSpan.Zero);

        Assert.Empty(await provider.DiscoverAsync(CancellationToken.None));
        Assert.Equal(SensorStatus.Unsupported, Assert.Single(await provider.ReadAsync([CpuTotal], CancellationToken.None)).Status);
    }

    [Fact]
    public async Task DisposeDisposesTheSource()
    {
        var source = new FakeSnapshotSource(FakeSnapshotSource.Snapshot());
        var provider = CreateProvider(source);
        await provider.DiscoverAsync(CancellationToken.None);

        provider.Dispose();

        Assert.True(source.Disposed);
    }

    [Fact]
    public void CpuMapperNormalizesUnitsAndKeepsEstimatedClockSeparate()
    {
        var snapshot = FakeSnapshotSource.Snapshot(cpu: new CpuRealtime
        {
            ClockMhz = 4500,
            LogicalProcessors = [new LogicalProcessorUsage(3, 50, 4200)],
        });

        var samples = new CpuSensorMapper().Map(snapshot).ToDictionary(s => s.Key.ToString());

        Assert.Equal(4_500_000_000, samples["cpu/0/clock/average-estimated"].Value);
        Assert.Equal(SensorUnit.Hertz, samples["cpu/0/clock/average-estimated"].Descriptor.Unit);
        Assert.Equal(50, samples["cpu/0/load/thread-3"].Value);
        Assert.Equal("threads", samples["cpu/0/load/thread-3"].Descriptor.Group);
        Assert.Equal(4_200_000_000, samples["cpu/0/clock/thread-3-estimated"].Value);
    }

    [Fact]
    public void MemoryMapperUsesTheWellKnownKey()
    {
        var snapshot = FakeSnapshotSource.Snapshot(memory: new MemoryRealtime { UsagePercent = 63, TotalBytes = 1024 });

        var samples = new MemorySensorMapper().Map(snapshot).ToDictionary(s => s.Key);

        Assert.Equal(63, samples[WellKnownSensorKeys.MemoryPhysicalLoad].Value);
        Assert.Equal(1024, samples[SensorKey.Parse("memory/0/data/total")].Value);
    }

    [Fact]
    public void GpuMapperIdentifiesGpusByPciLocationNotLuid()
    {
        GpuRealtime Gpu(ulong luid) => new(luid, "GPU A")
        {
            UsagePercent = 30,
            Telemetry = new GpuTelemetry(luid, "GPU A") { PciLocation = "01:00.0", CoreClockMhz = 2100 },
            Engines = [new GpuEngineUsage("Video Decode", 7)],
        };

        // LUID は再起動で変わる。
        var before = new GpuSensorMapper().Map(FakeSnapshotSource.Snapshot(gpus: [Gpu(1)])).Select(s => s.Key).ToArray();
        var after = new GpuSensorMapper().Map(FakeSnapshotSource.Snapshot(gpus: [Gpu(99)])).ToDictionary(s => s.Key);

        Assert.Equal(before, after.Keys);
        var id = HardwareIds.FromPersistentId("pci:01:00.0");
        Assert.Equal(30, after[SensorKey.Create(HardwareKind.Gpu, id, SensorType.Load, "max-engine")].Value);
        Assert.Equal(2_100_000_000, after[SensorKey.Create(HardwareKind.Gpu, id, SensorType.Clock, "core")].Value);
        Assert.Equal(7, after[SensorKey.Create(HardwareKind.Gpu, id, SensorType.Load, "engine-video-decode")].Value);
    }

    [Fact]
    public void StorageMapperSkipsDisksWithoutAPersistentIdentity()
    {
        var snapshot = FakeSnapshotSource.Snapshot(storage: new StorageRealtime(
            [new DiskRealtime(0, "SSD A", "C:") { ReadLatencyMilliseconds = 2 }, new DiskRealtime(1, "SSD B", "")],
            []));
        var mapper = new StorageSensorMapper(() => new Dictionary<int, string> { [0] = "serial:ABC" });

        var samples = mapper.Map(snapshot).ToArray();

        var id = HardwareIds.FromPersistentId("disk:serial:ABC");
        Assert.All(samples, s => Assert.Equal(id, s.Key.HardwareId));
        Assert.Equal("SSD A (C:)", samples[0].Descriptor.HardwareName);
        Assert.Equal(0.002, samples.Single(s => s.Key.Name == "read-latency").Value);
    }

    private static WindowsStandardProvider CreateProvider(FakeSnapshotSource source) =>
        new(() => source, [new CpuSensorMapper(), new MemorySensorMapper()], new ManualClock(), TimeSpan.Zero);
}

using Yamori.Core.Hardware;
using Yamori.Core.Hardware.Readers;
using Yamori.Core.Realtime;
using Yamori.Core.Realtime.Collectors;
using Yamori.Core.Realtime.Counters;
using Yamori.Core.Realtime.Gpu;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Realtime;

public class RealtimeCollectorTests
{
    [Fact]
    public void CpuCollectorOrdersLogicalProcessorsAndComputesClock()
    {
        var sampler = new FakeCounterSampler()
            .Set(CounterPaths.ProcessorTime, ("_Total", 40), ("0,_Total", 40), ("0,10", 80), ("0,2", 20), ("1,0", 120))
            .Set(CounterPaths.ProcessorPerformance, ("_Total", 150), ("0,10", 140), ("0,2", 100), ("1,0", 100))
            .Set(CounterPaths.ProcessorFrequency, ("_Total", 3000), ("0,10", 3000), ("0,2", 3000), ("1,0", 3000));
        var meters = new[]
        {
            new PowerMeterReading("rapl_package0_pkg", PowerMeterDomain.CpuPackage, 50),
            new PowerMeterReading("rapl_package0_pp0", PowerMeterDomain.CpuCores, 40),
        };

        var cpu = new CpuRealtimeCollector(sampler).Collect(null, meters);

        Assert.Equal(40, cpu.UsagePercent);
        Assert.Equal(4500, cpu.ClockMhz);
        Assert.Equal(50, cpu.PackagePowerWatts);
        Assert.Equal(40, cpu.CoresPowerWatts);
        Assert.Equal([20.0, 80.0, 100.0], cpu.LogicalProcessors.Select(p => p.UsagePercent));
        Assert.Equal([0, 1, 2], cpu.LogicalProcessors.Select(p => p.Index));
        Assert.Equal(4200, cpu.LogicalProcessors[1].ClockMhz);
    }

    [Fact]
    public void CpuCollectorPrefersProcessorUtilityAndKeepsProcessorTime()
    {
        var sampler = new FakeCounterSampler()
            .Set(CounterPaths.ProcessorTime, ("_Total", 40), ("0,0", 30), ("0,1", 50))
            .Set(CounterPaths.ProcessorUtility, ("_Total", 62), ("0,0", 120), ("0,1", 45));

        var cpu = new CpuRealtimeCollector(sampler).Collect(null, []);

        Assert.Equal(62, cpu.UsagePercent);
        Assert.Equal(40, cpu.ProcessorTimePercent);
        Assert.Equal([100.0, 45.0], cpu.LogicalProcessors.Select(p => p.UsagePercent));
    }

    [Fact]
    public void CpuCollectorFallsBackToProcessorTimeWithoutUtility()
    {
        var sampler = new FakeCounterSampler().Set(CounterPaths.ProcessorTime, ("_Total", 40), ("0,0", 30));

        var cpu = new CpuRealtimeCollector(sampler).Collect(null, []);

        Assert.Equal(40, cpu.UsagePercent);
        Assert.Equal(40, cpu.ProcessorTimePercent);
        Assert.Equal(30, Assert.Single(cpu.LogicalProcessors).UsagePercent);
    }

    [Fact]
    public void DiskCollectorUsesResolvedTemperature()
    {
        var sampler = new FakeCounterSampler().Set(CounterPaths.DiskIdleTime, ("0 C:", 90));
        var resolver = new StorageReliabilityResolver(
            () => new Dictionary<int, StorageReliability>(),
            new FakeStorageTemperatureSource(new Dictionary<int, int> { [0] = 38 }));
        var collector = new DiskRealtimeCollector(sampler, () => new Dictionary<int, string>(), () => resolver.Read([0]));

        Assert.Equal(38, Assert.Single(collector.Collect()).TemperatureCelsius);
    }

    [Fact]
    public void GpuCollectorSumsProcessesAndTakesBusiestEngine()
    {
        const string luid = "luid_0x00000000_0x0000F69A";
        var sampler = new FakeCounterSampler()
            .Set(
                CounterPaths.GpuEngineUtilization,
                ($"pid_1_{luid}_phys_0_eng_0_engtype_3D", 30),
                ($"pid_2_{luid}_phys_0_eng_0_engtype_3D", 25),
                ($"pid_1_{luid}_phys_0_eng_1_engtype_Copy", 5),
                ($"pid_1_{luid}_phys_0_eng_2_engtype_Copy", 12))
            .Set(CounterPaths.GpuDedicatedUsage, ($"{luid}_phys_0", 1024));

        var gpus = new GpuRealtimeCollector(sampler).Collect([new GpuTelemetry(0xF69A, "Test GPU") { DedicatedMemoryBytes = 4096 }]);

        var gpu = Assert.Single(gpus);
        Assert.Equal("Test GPU", gpu.Name);
        Assert.Equal(55, gpu.UsagePercent);
        Assert.Equal([new GpuEngineUsage("3D", 55), new GpuEngineUsage("Copy", 12)], gpu.Engines);
        Assert.Equal(1024UL, gpu.DedicatedUsedBytes);
    }

    [Fact]
    public void DiskCollectorConvertsIdleTimeAndLatency()
    {
        var sampler = new FakeCounterSampler()
            .Set(CounterPaths.DiskIdleTime, ("_Total", 90), ("1 C: D:", 75), ("0", 100))
            .Set(CounterPaths.DiskSecondsPerRead, ("1 C: D:", 0.004));
        var collector = new DiskRealtimeCollector(
            sampler,
            () => new Dictionary<int, string> { [1] = "Fast SSD" },
            () => new Dictionary<int, StorageReliability> { [1] = new(41, null, null) });

        var disks = collector.Collect();

        Assert.Equal([0, 1], disks.Select(d => d.DiskNumber));
        var disk = disks[1];
        Assert.Equal("Fast SSD", disk.Name);
        Assert.Equal("C: D:", disk.DriveLetters);
        Assert.Equal(25, disk.ActiveTimePercent);
        Assert.Equal(4, disk.ReadLatencyMilliseconds!.Value, 6);
        Assert.Equal(41, disk.TemperatureCelsius);
    }

    [Fact]
    public void CoolingCollectorConvertsKelvinAndKeepsStoppedGpuFans()
    {
        var sampler = new FakeCounterSampler()
            .Set(CounterPaths.ThermalZoneHighPrecisionTemperature, (@"\_TZ.TZ00", 3010))
            .Set(CounterPaths.ThermalZonePassiveLimit, (@"\_TZ.TZ00", 100))
            .Set(CounterPaths.ThermalZoneThrottleReasons, (@"\_TZ.TZ00", 0));
        var gpus = new[]
        {
            new GpuRealtime(1, "GPU A") { Telemetry = new GpuTelemetry(1, "GPU A") { FanRpm = 0, MaxFanRpm = 3000 } },
            new GpuRealtime(2, "GPU B") { Telemetry = new GpuTelemetry(2, "GPU B") },
        };

        var cooling = new CoolingRealtimeCollector(sampler).Collect(gpus);

        var fan = Assert.Single(cooling.Fans);
        Assert.Equal(new FanReading("GPU A", 0, 3000), fan);
        Assert.Equal(0, fan.SpeedPercent);
        var zone = Assert.Single(cooling.ThermalZones);
        Assert.Equal(27.9, zone.TemperatureCelsius, 6);
        Assert.False(zone.IsThrottling);
    }

    [Fact]
    public void PowerMeterReaderSkipsTotalsAndUnmeasuredDomains()
    {
        var sampler = new FakeCounterSampler()
            .Set(CounterPaths.EnergyMeterPower, ("_total", 0), ("RAPL_Package0_PKG", 64500), ("RAPL_Package0_DRAM", 0));

        var meters = new PowerMeterReader(sampler).Read();

        var meter = Assert.Single(meters);
        Assert.Equal(PowerMeterDomain.CpuPackage, meter.Domain);
        Assert.Equal(64.5, meter.Watts, 6);
    }

    [Theory]
    [InlineData("rapl_package0_pp0", PowerMeterDomain.CpuCores)]
    [InlineData("RAPL_Package0_PP1", PowerMeterDomain.CpuGraphics)]
    [InlineData("rapl_package0_dram", PowerMeterDomain.Memory)]
    [InlineData("rapl_package0_psys", PowerMeterDomain.Platform)]
    [InlineData("acpi_meter", PowerMeterDomain.Other)]
    public void ClassifiesPowerMeterDomains(string name, PowerMeterDomain expected)
    {
        Assert.Equal(expected, PowerMeterReader.ClassifyDomain(name));
    }
}

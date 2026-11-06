using Yamori.Core.Hardware;
using Yamori.Core.Tests.Fakes;
using Row = System.Collections.Generic.Dictionary<string, object?>;

namespace Yamori.Core.Tests.Hardware;

public class HardwareInventoryServiceTests
{
    [Fact]
    public void ReadsAllComponentsFromWmi()
    {
        var wmi = new FakeWmiQueryExecutor()
            .With("Win32_Processor", new Row { ["Name"] = " Test CPU ", ["NumberOfCores"] = 8u, ["NumberOfLogicalProcessors"] = 16u, ["MaxClockSpeed"] = 3200u })
            .With(
                "Win32_PhysicalMemory",
                new Row { ["Capacity"] = 8UL << 30, ["ConfiguredClockSpeed"] = 5600u },
                new Row { ["Capacity"] = 8UL << 30, ["ConfiguredClockSpeed"] = 4800u })
            .With("MSFT_PhysicalDisk", new Row { ["FriendlyName"] = "Test SSD", ["Size"] = 1UL << 40, ["MediaType"] = (ushort)4, ["BusType"] = (ushort)17, ["HealthStatus"] = (ushort)0 })
            .With("Win32_VideoController", new Row { ["Name"] = "Test GPU", ["AdapterRAM"] = 0u, ["DriverVersion"] = "1.0" })
            .With("Win32_Battery", new Row { ["Name"] = "Test Battery", ["EstimatedChargeRemaining"] = (ushort)70 })
            .With("BatteryStaticData", new Row { ["DesignedCapacity"] = 50000u })
            .With("BatteryFullChargedCapacity", new Row { ["FullChargedCapacity"] = 40000u });

        var inventory = HardwareInventoryService.CreateForWmi(wmi).GetInventory();

        Assert.Equal(new CpuInfo("Test CPU", 8, 16, 3200), inventory.Cpu);
        Assert.Equal(new MemoryInfo(16UL << 30, 2, 4800), inventory.Memory);

        var disk = Assert.Single(inventory.Storage);
        Assert.Equal(StorageMediaType.Ssd, disk.MediaType);
        Assert.Equal("NVMe", disk.BusType);
        Assert.Equal(StorageHealth.Healthy, disk.Health);

        Assert.Null(Assert.Single(inventory.Gpus).AdapterRamBytes);

        var battery = Assert.Single(inventory.Batteries);
        Assert.Equal(70, battery.ChargePercent);
        Assert.Equal(0.8, battery.HealthRatio!.Value, precision: 10);
    }

    [Fact]
    public void KeepsOtherComponentsWhenOneQueryFails()
    {
        var wmi = new FakeWmiQueryExecutor()
            .Failing("MSFT_PhysicalDisk")
            .With("Win32_Processor", new Row { ["Name"] = "CPU", ["NumberOfCores"] = 4u, ["NumberOfLogicalProcessors"] = 8u, ["MaxClockSpeed"] = 2000u });

        var inventory = HardwareInventoryService.CreateForWmi(wmi).GetInventory();

        Assert.NotNull(inventory.Cpu);
        Assert.Empty(inventory.Storage);
        Assert.Null(inventory.Memory);
    }

    [Fact]
    public void ReturnsBatteryWithoutCapacityWhenCapacityClassesFail()
    {
        var wmi = new FakeWmiQueryExecutor()
            .With("Win32_Battery", new Row { ["Name"] = "Battery", ["EstimatedChargeRemaining"] = (ushort)50 })
            .Failing("BatteryStaticData")
            .Failing("BatteryFullChargedCapacity");

        var battery = Assert.Single(HardwareInventoryService.CreateForWmi(wmi).GetInventory().Batteries);

        Assert.Equal(50, battery.ChargePercent);
        Assert.Null(battery.HealthRatio);
    }
}

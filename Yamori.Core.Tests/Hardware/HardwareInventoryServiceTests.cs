using Yamori.Core.Hardware;
using Yamori.Core.Hardware.Graphics;
using Yamori.Core.Hardware.Readers;
using Yamori.Core.Hardware.Topology;
using Yamori.Core.Tests.Fakes;
using Row = System.Collections.Generic.Dictionary<string, object?>;

namespace Yamori.Core.Tests.Hardware;

public class HardwareInventoryServiceTests
{
    private static HardwareInventoryService Create(
        FakeWmiQueryExecutor wmi,
        byte[]? smbios = null,
        CoreTopology? topology = null,
        FakeStorageTemperatureSource? temperature = null,
        params GraphicsAdapterDetails[] adapters) =>
        HardwareInventoryService.CreateDefault(
            wmi,
            new FakeSmbiosTableSource(smbios),
            new FakeGraphicsAdapterSource(adapters),
            new FakeCoreTopologySource(topology),
            temperature ?? new FakeStorageTemperatureSource());

    [Fact]
    public void FillsMissingDiskTemperatureFromDirectQuery()
    {
        var wmi = new FakeWmiQueryExecutor()
            .With(
                "MSFT_PhysicalDisk",
                new Row { ["DeviceId"] = "0", ["FriendlyName"] = "HDD", ["Size"] = 8UL << 40, ["MediaType"] = (ushort)3, ["BusType"] = (ushort)11, ["HealthStatus"] = (ushort)0 },
                new Row { ["DeviceId"] = "1", ["FriendlyName"] = "SSD", ["Size"] = 2UL << 40, ["MediaType"] = (ushort)4, ["BusType"] = (ushort)17, ["HealthStatus"] = (ushort)0 })
            .With("MSFT_StorageReliabilityCounter", new Row { ["DeviceId"] = "1", ["Temperature"] = (ushort)41, ["Wear"] = (byte)3 });
        var temperature = new FakeStorageTemperatureSource(new Dictionary<int, int> { [0] = 31, [1] = 99 });

        var storage = Create(wmi, temperature: temperature).GetInventory().Storage;

        Assert.Equal(new StorageReliability(31, null, null), storage.Single(d => d.Model == "HDD").Reliability);
        Assert.Equal(new StorageReliability(41, 3, null), storage.Single(d => d.Model == "SSD").Reliability);
        Assert.Equal([0], temperature.Requested);
    }

    [Fact]
    public void ReadsNetworkAdaptersWithoutPersonalData()
    {
        var wmi = new FakeWmiQueryExecutor()
            .With(
                "MSFT_NetAdapter",
                new Row { ["InterfaceDescription"] = "Contoso Wi-Fi 7", ["HardwareInterface"] = true, ["Virtual"] = false, ["NdisPhysicalMedium"] = 9u, ["InterfaceType"] = 71u, ["MediaConnectState"] = 1u, ["Speed"] = 2_400_000_000UL, ["DriverVersionString"] = "23.1" },
                new Row { ["InterfaceDescription"] = "Hyper-V Virtual Ethernet Adapter", ["HardwareInterface"] = false, ["Virtual"] = true, ["NdisPhysicalMedium"] = 14u, ["InterfaceType"] = 6u, ["MediaConnectState"] = 1u, ["Speed"] = 10_000_000_000UL },
                new Row { ["InterfaceDescription"] = "Contoso 2.5GbE", ["HardwareInterface"] = true, ["Virtual"] = false, ["NdisPhysicalMedium"] = 14u, ["InterfaceType"] = 6u, ["MediaConnectState"] = 2u, ["Speed"] = 0UL });

        var adapters = Create(wmi).GetInventory().NetworkAdapters;

        Assert.Equal(["Contoso 2.5GbE", "Contoso Wi-Fi 7", "Hyper-V Virtual Ethernet Adapter"], adapters.Select(a => a.Name));
        var wifi = adapters[1];
        Assert.Equal((NetworkConnectionKind.WiFi, true, true, 2_400_000_000UL), (wifi.ConnectionKind, wifi.IsPhysical, wifi.IsConnected, wifi.LinkSpeedBitsPerSecond!.Value));
        Assert.Equal("23.1", wifi.DriverVersion);
        Assert.Null(adapters[0].LinkSpeedBitsPerSecond);
        Assert.False(adapters[0].IsConnected);
        Assert.False(adapters[2].IsPhysical);
        Assert.DoesNotContain(wmi.Queries, q => q.Contains("MacAddress", StringComparison.OrdinalIgnoreCase) || q.Contains("PermanentAddress", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(14, 6, NetworkConnectionKind.Ethernet)]
    [InlineData(9, 71, NetworkConnectionKind.WiFi)]
    [InlineData(8, 243, NetworkConnectionKind.Cellular)]
    [InlineData(10, 6, NetworkConnectionKind.Bluetooth)]
    [InlineData(0, 71, NetworkConnectionKind.WiFi)]
    [InlineData(0, 131, NetworkConnectionKind.Other)]
    public void ClassifiesNetworkConnectionKind(int medium, int interfaceType, NetworkConnectionKind expected)
    {
        Assert.Equal(expected, NetworkAdapterInfoReader.ToConnectionKind(medium, interfaceType));
    }

    [Fact]
    public void ReadsAllComponentsFromWmi()
    {
        var wmi = new FakeWmiQueryExecutor()
            .With("Win32_Processor", new Row { ["Name"] = " Test CPU ", ["NumberOfCores"] = 8u, ["NumberOfLogicalProcessors"] = 16u, ["MaxClockSpeed"] = 3200u, ["Architecture"] = (ushort)9, ["SocketDesignation"] = "AM5", ["L3CacheSize"] = 32768u })
            .With(
                "Win32_PhysicalMemory",
                new Row { ["Capacity"] = 8UL << 30, ["ConfiguredClockSpeed"] = 5600u, ["SMBIOSMemoryType"] = 34u, ["FormFactor"] = (ushort)8 },
                new Row { ["Capacity"] = 8UL << 30, ["ConfiguredClockSpeed"] = 4800u })
            .With("MSFT_PhysicalDisk", new Row { ["DeviceId"] = "0", ["FriendlyName"] = "Test SSD", ["Size"] = 1UL << 40, ["MediaType"] = (ushort)4, ["BusType"] = (ushort)17, ["HealthStatus"] = (ushort)0 })
            .With("Win32_VideoController", new Row { ["Name"] = "Test GPU", ["AdapterRAM"] = 0u, ["DriverVersion"] = "1.0" })
            .With("Win32_Battery", new Row { ["Name"] = "Test Battery", ["EstimatedChargeRemaining"] = (ushort)70 })
            .With("BatteryStaticData", new Row { ["DesignedCapacity"] = 50000u })
            .With("BatteryFullChargedCapacity", new Row { ["FullChargedCapacity"] = 40000u })
            .With("BatteryCycleCount", new Row { ["CycleCount"] = 120u });

        var inventory = Create(wmi, topology: new CoreTopology(6, 2)).GetInventory();

        var cpu = inventory.Cpu!;
        Assert.Equal(("Test CPU", 8, 16, 3200), (cpu.Name, cpu.PhysicalCores, cpu.LogicalProcessors, cpu.MaxClockMhz));
        Assert.Equal("x64", cpu.Architecture);
        Assert.Equal("AM5", cpu.Socket);
        Assert.Equal((6, 2), (cpu.PerformanceCores!.Value, cpu.EfficiencyCores!.Value));
        Assert.Equal(32UL << 20, cpu.L3CacheBytes);

        var memory = inventory.Memory!;
        Assert.Equal((16UL << 30, 2, 4800), (memory.TotalBytes, memory.ModuleCount, memory.SpeedMhz!.Value));
        Assert.Equal("DDR5", memory.TypeName);
        Assert.Equal("DIMM", memory.Modules[0].FormFactor);

        var disk = Assert.Single(inventory.Storage);
        Assert.Equal(StorageMediaType.Ssd, disk.MediaType);
        Assert.Equal("NVMe", disk.BusType);
        Assert.Equal(StorageHealth.Healthy, disk.Health);

        Assert.Null(Assert.Single(inventory.Gpus).DedicatedMemoryBytes);

        var battery = Assert.Single(inventory.PowerSupply.Batteries);
        Assert.Equal(70, battery.ChargePercent);
        Assert.Equal(120, battery.CycleCount);
        Assert.Equal(0.8, battery.HealthRatio!.Value, precision: 10);
    }

    [Fact]
    public void KeepsOtherComponentsWhenOneQueryFails()
    {
        var wmi = new FakeWmiQueryExecutor()
            .Failing("MSFT_PhysicalDisk")
            .With("Win32_Processor", new Row { ["Name"] = "CPU", ["NumberOfCores"] = 4u, ["NumberOfLogicalProcessors"] = 8u, ["MaxClockSpeed"] = 2000u });

        var inventory = Create(wmi).GetInventory();

        Assert.NotNull(inventory.Cpu);
        Assert.Empty(inventory.Storage);
        Assert.Null(inventory.Memory);
        Assert.Null(inventory.Motherboard);
        Assert.Empty(inventory.Fans);
    }

    [Fact]
    public void ReturnsBatteryWithoutCapacityWhenCapacityClassesFail()
    {
        var wmi = new FakeWmiQueryExecutor()
            .With("Win32_Battery", new Row { ["Name"] = "Battery", ["EstimatedChargeRemaining"] = (ushort)50 })
            .Failing("BatteryStaticData")
            .Failing("BatteryFullChargedCapacity")
            .Failing("BatteryCycleCount");

        var battery = Assert.Single(Create(wmi).GetInventory().PowerSupply.Batteries);

        Assert.Equal(50, battery.ChargePercent);
        Assert.Null(battery.HealthRatio);
        Assert.Null(battery.CycleCount);
    }

    [Fact]
    public void MovesIntegratedGpuIntoCpuAndUsesExactVideoMemory()
    {
        var wmi = new FakeWmiQueryExecutor()
            .With("Win32_Processor", new Row { ["Name"] = "CPU", ["NumberOfCores"] = 8u, ["NumberOfLogicalProcessors"] = 16u, ["MaxClockSpeed"] = 3000u })
            .With(
                "Win32_VideoController",
                new Row { ["Name"] = "Integrated", ["AdapterRAM"] = 128u << 20, ["DriverVersion"] = "1", ["PNPDeviceID"] = @"PCI\VEN_8086&DEV_A780&SUBSYS_00000000&REV_04\3&11583659&0&10" },
                new Row { ["Name"] = "Discrete", ["AdapterRAM"] = 4293918720u, ["DriverVersion"] = "2", ["PNPDeviceID"] = @"PCI\VEN_10DE&DEV_2203&SUBSYS_50911462&REV_A1\4&22F77229&0&0008" });

        var inventory = Create(
            wmi,
            adapters:
            [
                new GraphicsAdapterDetails(new PciDeviceId(0x10DE, 0x2203), 24UL << 30, 32UL << 30, IsIntegrated: false),
                new GraphicsAdapterDetails(new PciDeviceId(0x8086, 0xA780), 128UL << 20, 32UL << 30, IsIntegrated: true),
            ]).GetInventory();

        Assert.Equal("Integrated", inventory.Cpu!.IntegratedGpu!.Name);
        var discrete = Assert.Single(inventory.Gpus);
        Assert.Equal("Discrete", discrete.Name);
        Assert.Equal(24UL << 30, discrete.DedicatedMemoryBytes);
        Assert.False(discrete.IsDedicatedMemoryCapped);
    }

    [Fact]
    public void MarksWmiVideoMemoryAsCappedWithoutDxCore()
    {
        var wmi = new FakeWmiQueryExecutor()
            .With("Win32_VideoController", new Row { ["Name"] = "GPU", ["AdapterRAM"] = 4293918720u, ["DriverVersion"] = "1", ["PNPDeviceID"] = @"PCI\VEN_10DE&DEV_2203" });

        var gpu = Assert.Single(Create(wmi).GetInventory().Gpus);

        Assert.Equal(4293918720UL, gpu.DedicatedMemoryBytes);
        Assert.True(gpu.IsDedicatedMemoryCapped);
        Assert.Null(gpu.IsIntegrated);
    }

    [Fact]
    public void ReadsMotherboardAndDropsOemPlaceholders()
    {
        var wmi = new FakeWmiQueryExecutor()
            .With("Win32_BaseBoard", new Row { ["Manufacturer"] = "ASUSTeK COMPUTER INC.", ["Product"] = "ROG STRIX Z690-F", ["Version"] = "Rev 1.xx" })
            .With("Win32_BIOS", new Row { ["Manufacturer"] = "American Megatrends Inc.", ["SMBIOSBIOSVersion"] = "4001", ["ReleaseDate"] = "20241004000000.000000+000", ["SMBIOSMajorVersion"] = (ushort)3, ["SMBIOSMinorVersion"] = (ushort)4 })
            .With("Win32_ComputerSystem", new Row { ["Manufacturer"] = "ASUS", ["Model"] = "System Product Name" })
            .With("Win32_SystemEnclosure", new Row { ["ChassisTypes"] = new ushort[] { 3 } });

        var board = Create(wmi).GetInventory().Motherboard!;

        Assert.Equal("ROG STRIX Z690-F", board.Product);
        Assert.Null(board.SystemModel);
        Assert.Equal(ChassisKind.Desktop, board.Chassis);
        Assert.Equal(new DateOnly(2024, 10, 4), board.BiosReleaseDate);
        Assert.Equal("3.4", board.SmbiosVersion);
    }

    [Fact]
    public void ReadsMemorySlotsAndMaximumCapacity()
    {
        var wmi = new FakeWmiQueryExecutor()
            .With("Win32_PhysicalMemory", new Row { ["DeviceLocator"] = "DIMM1", ["Capacity"] = 32UL << 30, ["Speed"] = 4800u })
            .With(
                "Win32_PhysicalMemoryArray",
                new Row { ["Use"] = (ushort)3, ["MemoryDevices"] = (ushort)4, ["MaxCapacityEx"] = 256UL << 20 },
                new Row { ["Use"] = (ushort)7, ["MemoryDevices"] = (ushort)1, ["MaxCapacityEx"] = 1UL << 20 });

        var memory = Create(wmi).GetInventory().Memory!;

        Assert.Equal(4, memory.SlotCount);
        Assert.Equal(3, memory.FreeSlots);
        Assert.Equal(256UL << 30, memory.MaxCapacityBytes);
        Assert.Equal("DIMM1", Assert.Single(memory.Modules).Slot);
    }

    [Fact]
    public void AttachesVolumesAndReliabilityToDisks()
    {
        var wmi = new FakeWmiQueryExecutor()
            .With(
                "MSFT_PhysicalDisk",
                new Row { ["DeviceId"] = "1", ["FriendlyName"] = "SSD", ["Size"] = 2UL << 40, ["MediaType"] = (ushort)4, ["BusType"] = (ushort)17, ["HealthStatus"] = (ushort)0 },
                new Row { ["DeviceId"] = "0", ["FriendlyName"] = "HDD", ["Size"] = 8UL << 40, ["MediaType"] = (ushort)3, ["BusType"] = (ushort)11, ["HealthStatus"] = (ushort)0, ["SpindleSpeed"] = 7200u })
            .With(
                "MSFT_Partition",
                new Row { ["DiskNumber"] = 1u, ["DriveLetter"] = 'C' },
                new Row { ["DiskNumber"] = 1u, ["DriveLetter"] = '\0' })
            .With("Win32_LogicalDisk", new Row { ["DeviceID"] = "C:", ["Size"] = 2UL << 40, ["FreeSpace"] = 1UL << 40, ["FileSystem"] = "NTFS" })
            .With("MSFT_StorageReliabilityCounter", new Row { ["DeviceId"] = "1", ["Temperature"] = (ushort)41, ["Wear"] = (byte)3, ["PowerOnHours"] = 1200u });

        var storage = Create(wmi).GetInventory().Storage;

        var ssd = storage.Single(d => d.Model == "SSD");
        var volume = Assert.Single(ssd.Volumes);
        Assert.Equal('C', volume.DriveLetter);
        Assert.Equal(0.5, volume.UsedRatio, precision: 10);
        Assert.Equal(new StorageReliability(41, 3, 1200), ssd.Reliability);

        var hdd = storage.Single(d => d.Model == "HDD");
        Assert.Equal(7200, hdd.SpindleSpeedRpm);
        Assert.Empty(hdd.Volumes);
        Assert.Null(hdd.Reliability);
    }

    [Fact]
    public void ReadsFansAndPowerSupplyFromSmbios()
    {
        var smbios = SmbiosBytes.Build(
            ([27, 0x0F, 0x3D, 0x00, 0x3A, 0x00, 0x66, 0x01, 0, 0, 0, 0, 0xB0, 0x04, 0x01], ["CPU Fan"]),
            ([27, 0x0F, 0x40, 0x00, 0x3A, 0x00, 0x67, 0x01, 0, 0, 0, 0, 0x00, 0x80, 0x00], []),
            ([39, 0x16, 0x4A, 0x00, 0x01, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0xF4, 0x01, 0xA2, 0x11, 0x46, 0x00, 0x48, 0x00, 0x49, 0x00],
                ["To Be Filled By O.E.M.", "PSU1", "Contoso", "SN", "Asset", "CT-500", "1.0"]));

        var inventory = Create(new FakeWmiQueryExecutor(), smbios).GetInventory();

        Assert.Equal(2, inventory.Fans.Count);
        Assert.Equal(new FanInfo("CPU Fan", CoolingDeviceType.CabinetFan, DeviceStatus.Ok, 1200), inventory.Fans[0]);
        Assert.Equal(new FanInfo(null, CoolingDeviceType.PowerSupplyFan, DeviceStatus.Ok, null), inventory.Fans[1]);

        var unit = Assert.Single(inventory.PowerSupply.Units);
        Assert.Equal(new PowerSupplyUnitInfo("PSU1", "Contoso", "CT-500", 500, PowerSupplyKind.Switching, DeviceStatus.Ok, false), unit);
    }

    [Fact]
    public void ReturnsNoFansOrUnitsWithoutSmbios()
    {
        var inventory = Create(new FakeWmiQueryExecutor(), smbios: null).GetInventory();

        Assert.Empty(inventory.Fans);
        Assert.Empty(inventory.PowerSupply.Units);
    }
}

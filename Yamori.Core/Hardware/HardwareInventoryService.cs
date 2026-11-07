using Yamori.Core.Hardware.DeviceIo;
using Yamori.Core.Hardware.Graphics;
using Yamori.Core.Hardware.Readers;
using Yamori.Core.Hardware.Smbios;
using Yamori.Core.Hardware.Topology;
using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware;

/// <summary>
/// 部品ごとの読み取り処理をまとめて、部品の種類（<see cref="HardwareComponent"/>）ごとに分類したハードウェア一覧を作る。
/// </summary>
public sealed class HardwareInventoryService : IHardwareInventoryProvider
{
    private readonly HardwareReaderSet _readers;

    /// <summary>
    /// 部品ごとの読み取り処理を指定して作る。
    /// </summary>
    /// <param name="readers">部品ごとの読み取り処理。</param>
    /// <exception cref="ArgumentNullException"><paramref name="readers"/> が null。</exception>
    public HardwareInventoryService(HardwareReaderSet readers)
    {
        ArgumentNullException.ThrowIfNull(readers);
        _readers = readers;
    }

    /// <summary>
    /// Windows が標準で提供する仕組み（WMI、SMBIOS、DXCore、Win32 API）を情報源とする標準の構成で作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <param name="smbios">SMBIOS テーブルの情報源。省略時はファームウェアから読む。</param>
    /// <param name="graphics">GPU の詳しい情報の情報源。省略時は DXCore から読む。</param>
    /// <param name="topology">CPU のコアの内訳の情報源。省略時は Win32 API から読む。</param>
    /// <param name="storageTemperature">ストレージの温度を直接問い合わせる情報源。省略時は IOCTL で読む。</param>
    /// <returns>標準の情報源から読み取る <see cref="HardwareInventoryService"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public static HardwareInventoryService CreateDefault(
        IWmiQueryExecutor wmi,
        ISmbiosTableSource? smbios = null,
        IGraphicsAdapterSource? graphics = null,
        ICoreTopologySource? topology = null,
        IStorageTemperatureSource? storageTemperature = null)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        smbios ??= new FirmwareSmbiosTableSource();

        return new HardwareInventoryService(new HardwareReaderSet(
            Cpu: new CpuInfoReader(wmi, topology ?? new Win32CoreTopologySource()),
            Motherboard: new MotherboardInfoReader(wmi),
            Memory: new MemoryInfoReader(wmi),
            Gpu: new GpuInfoReader(wmi, graphics ?? new DxCoreGraphicsAdapterSource()),
            Storage: new StorageInfoReader(wmi, storageTemperature ?? new IoctlStorageTemperatureSource()),
            Fan: new FanInfoReader(smbios),
            PowerSupply: new PowerSupplyInfoReader(
                new PowerSupplyUnitReader(smbios),
                new BatteryInfoReader(wmi),
                new PowerSourceReader()),
            Network: new NetworkAdapterInfoReader(wmi)));
    }

    /// <inheritdoc />
    public HardwareInventory GetInventory()
    {
        var (cpu, gpus) = IntegratedGpuAssigner.Assign(
            _readers.Cpu.ReadOrDefault(null),
            _readers.Gpu.ReadOrDefault([]));

        return new HardwareInventory(
            Cpu: cpu,
            Motherboard: _readers.Motherboard.ReadOrDefault(null),
            Memory: _readers.Memory.ReadOrDefault(null),
            Gpus: gpus,
            Storage: _readers.Storage.ReadOrDefault([]),
            Fans: _readers.Fan.ReadOrDefault([]),
            PowerSupply: _readers.PowerSupply.ReadOrDefault(PowerSupplyInfo.Empty),
            NetworkAdapters: _readers.Network.ReadOrDefault([]));
    }
}

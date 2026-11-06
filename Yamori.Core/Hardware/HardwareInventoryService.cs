using Yamori.Core.Hardware.Readers;
using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware;

/// <summary>
/// 部品ごとの読み取り処理をまとめて、1 つのハードウェア一覧を作る。
/// </summary>
public sealed class HardwareInventoryService : IHardwareInventoryProvider
{
    private readonly IHardwareReader<CpuInfo?> _cpuReader;
    private readonly IHardwareReader<MemoryInfo?> _memoryReader;
    private readonly IHardwareReader<IReadOnlyList<StorageDeviceInfo>> _storageReader;
    private readonly IHardwareReader<IReadOnlyList<GpuInfo>> _gpuReader;
    private readonly IHardwareReader<IReadOnlyList<BatteryInfo>> _batteryReader;

    /// <summary>
    /// 部品ごとの読み取り処理を指定して作る。
    /// </summary>
    /// <param name="cpuReader">CPU の読み取り処理。</param>
    /// <param name="memoryReader">メモリの読み取り処理。</param>
    /// <param name="storageReader">ストレージの読み取り処理。</param>
    /// <param name="gpuReader">GPU の読み取り処理。</param>
    /// <param name="batteryReader">バッテリーの読み取り処理。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public HardwareInventoryService(
        IHardwareReader<CpuInfo?> cpuReader,
        IHardwareReader<MemoryInfo?> memoryReader,
        IHardwareReader<IReadOnlyList<StorageDeviceInfo>> storageReader,
        IHardwareReader<IReadOnlyList<GpuInfo>> gpuReader,
        IHardwareReader<IReadOnlyList<BatteryInfo>> batteryReader)
    {
        ArgumentNullException.ThrowIfNull(cpuReader);
        ArgumentNullException.ThrowIfNull(memoryReader);
        ArgumentNullException.ThrowIfNull(storageReader);
        ArgumentNullException.ThrowIfNull(gpuReader);
        ArgumentNullException.ThrowIfNull(batteryReader);

        _cpuReader = cpuReader;
        _memoryReader = memoryReader;
        _storageReader = storageReader;
        _gpuReader = gpuReader;
        _batteryReader = batteryReader;
    }

    /// <summary>
    /// WMI を情報源とする標準の構成で作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <returns>WMI から読み取る <see cref="HardwareInventoryService"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public static HardwareInventoryService CreateForWmi(IWmiQueryExecutor wmi)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        return new HardwareInventoryService(
            new CpuInfoReader(wmi),
            new MemoryInfoReader(wmi),
            new StorageInfoReader(wmi),
            new GpuInfoReader(wmi),
            new BatteryInfoReader(wmi));
    }

    /// <inheritdoc />
    public HardwareInventory GetInventory() =>
        new(
            Cpu: ReadOrDefault(_cpuReader, null),
            Memory: ReadOrDefault(_memoryReader, null),
            Storage: ReadOrDefault(_storageReader, []),
            Gpus: ReadOrDefault(_gpuReader, []),
            Batteries: ReadOrDefault(_batteryReader, []));

    private static T ReadOrDefault<T>(IHardwareReader<T> reader, T fallback)
    {
        try
        {
            return reader.Read();
        }
        catch (WmiQueryException)
        {
            // 1 つの部品が読めなくても、他の部品の表示は続ける。
            return fallback;
        }
    }
}

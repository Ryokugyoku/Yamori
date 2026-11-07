using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// Win32_PhysicalMemory から搭載メモリとモジュールごとの情報を、Win32_PhysicalMemoryArray からスロット数と最大容量を読み取る。
/// </summary>
public sealed class MemoryInfoReader : IHardwareReader<MemoryInfo?>
{
    private const string ModuleQuery =
        "SELECT DeviceLocator, BankLabel, Capacity, Speed, ConfiguredClockSpeed, SMBIOSMemoryType, FormFactor, Manufacturer, PartNumber, ConfiguredVoltage FROM Win32_PhysicalMemory";

    private const string ArrayQuery = "SELECT Use, MemoryDevices, MaxCapacityEx, MaxCapacity FROM Win32_PhysicalMemoryArray";

    /// <summary>Win32_PhysicalMemoryArray.Use の「システムメモリ」。</summary>
    private const int SystemMemoryUse = 3;

    private const ulong BytesPerKilobyte = 1024;

    private readonly IWmiQueryExecutor _wmi;

    /// <summary>
    /// 読み取りに使う WMI 実行器を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public MemoryInfoReader(IWmiQueryExecutor wmi)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        _wmi = wmi;
    }

    /// <inheritdoc />
    /// <returns>メモリの情報。モジュールが見つからない場合は null。</returns>
    public MemoryInfo? Read()
    {
        var rows = _wmi.Query(WmiNamespaces.Cimv2, ModuleQuery);
        if (rows.Count == 0)
        {
            return null;
        }

        var modules = rows.Select(ToModule).ToArray();
        var total = modules.Aggregate(0UL, (sum, m) => sum + m.CapacityBytes);
        var speed = modules
            .Select(m => m.ConfiguredSpeedMtps ?? m.RatedSpeedMtps)
            .Where(s => s is > 0)
            .DefaultIfEmpty(null)
            .Min();

        var arrays = _wmi.QueryOrEmpty(WmiNamespaces.Cimv2, ArrayQuery)
            .Where(r => r.GetInt32("Use") is null or SystemMemoryUse)
            .ToArray();

        return new MemoryInfo(total, modules.Length, speed)
        {
            Modules = modules,
            SlotCount = arrays.Length == 0 ? null : arrays.Sum(r => r.GetInt32("MemoryDevices") ?? 0) is > 0 and var slots ? slots : null,
            MaxCapacityBytes = arrays.Length == 0 ? null : arrays.Aggregate(0UL, (sum, r) => sum + MaxCapacityKilobytes(r)) is > 0 and var max ? max * BytesPerKilobyte : null,
        };
    }

    private static MemoryModuleInfo ToModule(WmiRecord row, int index) =>
        new(
            Slot: OemPlaceholderFilter.Clean(row.GetString("DeviceLocator"))
                ?? OemPlaceholderFilter.Clean(row.GetString("BankLabel"))
                ?? $"#{index + 1}",
            CapacityBytes: row.GetUInt64("Capacity") ?? 0)
        {
            RatedSpeedMtps = row.GetInt32("Speed") is > 0 and var rated ? rated : null,
            ConfiguredSpeedMtps = row.GetInt32("ConfiguredClockSpeed") is > 0 and var configured ? configured : null,
            TypeName = MemoryTypeNames.GetTypeName(row.GetInt32("SMBIOSMemoryType")),
            FormFactor = MemoryTypeNames.GetFormFactorName(row.GetInt32("FormFactor")),
            Manufacturer = OemPlaceholderFilter.Clean(row.GetString("Manufacturer")),
            PartNumber = OemPlaceholderFilter.Clean(row.GetString("PartNumber")),
            ConfiguredVoltageMillivolts = row.GetInt32("ConfiguredVoltage") is > 0 and var voltage ? voltage : null,
        };

    private static ulong MaxCapacityKilobytes(WmiRecord row) =>
        row.GetUInt64("MaxCapacityEx") is > 0 and var extended ? extended : row.GetUInt64("MaxCapacity") ?? 0;
}

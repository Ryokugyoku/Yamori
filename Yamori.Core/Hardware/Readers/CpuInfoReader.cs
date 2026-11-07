using Yamori.Core.Hardware.Topology;
using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// Win32_Processor から CPU の情報を読み取る。複数ソケットの場合はコア数とキャッシュを合計する。
/// P コア・E コアの内訳は <see cref="ICoreTopologySource"/> から読む。
/// </summary>
/// <remarks>
/// 内蔵 GPU はここでは読まない。GPU の一覧から <see cref="HardwareInventoryService"/> が振り分ける。
/// </remarks>
public sealed class CpuInfoReader : IHardwareReader<CpuInfo?>
{
    private const string Query =
        "SELECT Name, Manufacturer, Architecture, SocketDesignation, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, L2CacheSize, L3CacheSize FROM Win32_Processor";

    private const ulong BytesPerKilobyte = 1024;

    private static readonly IReadOnlyDictionary<int, string> ArchitectureNames = new Dictionary<int, string>
    {
        [0] = "x86",
        [5] = "ARM",
        [9] = "x64",
        [12] = "ARM64",
    };

    private readonly IWmiQueryExecutor _wmi;
    private readonly ICoreTopologySource? _topology;

    /// <summary>
    /// 読み取りに使う情報源を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <param name="topology">コアの内訳の情報源。省略した場合、内訳は読まない。</param>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public CpuInfoReader(IWmiQueryExecutor wmi, ICoreTopologySource? topology = null)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        _wmi = wmi;
        _topology = topology;
    }

    /// <inheritdoc />
    /// <returns>CPU の情報。WMI に CPU が見つからない場合は null。</returns>
    public CpuInfo? Read()
    {
        var rows = _wmi.Query(WmiNamespaces.Cimv2, Query);
        if (rows.Count == 0)
        {
            return null;
        }

        var first = rows[0];
        var topology = _topology?.Read();
        return new CpuInfo(
            Name: first.GetString("Name") ?? "不明な CPU",
            PhysicalCores: rows.Sum(r => r.GetInt32("NumberOfCores") ?? 0),
            LogicalProcessors: rows.Sum(r => r.GetInt32("NumberOfLogicalProcessors") ?? 0),
            MaxClockMhz: rows.Max(r => r.GetInt32("MaxClockSpeed") ?? 0))
        {
            Manufacturer = first.GetString("Manufacturer"),
            Architecture = first.GetInt32("Architecture") is { } arch && ArchitectureNames.TryGetValue(arch, out var name) ? name : null,
            Socket = OemPlaceholderFilter.Clean(first.GetString("SocketDesignation")),
            SocketCount = rows.Count,
            PerformanceCores = topology?.PerformanceCores,
            EfficiencyCores = topology?.EfficiencyCores,
            L2CacheBytes = SumKilobytes(rows, "L2CacheSize"),
            L3CacheBytes = SumKilobytes(rows, "L3CacheSize"),
        };
    }

    private static ulong? SumKilobytes(IReadOnlyList<WmiRecord> rows, string property)
    {
        var total = rows.Aggregate(0UL, (sum, r) => sum + (r.GetUInt64(property) ?? 0));
        return total > 0 ? total * BytesPerKilobyte : null;
    }
}

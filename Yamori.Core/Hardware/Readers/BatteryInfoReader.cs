using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// Win32_Battery と root\WMI のバッテリー容量クラスから、バッテリーの情報を読み取る。
/// </summary>
/// <remarks>
/// 容量や充放電回数の情報は機種によって取れないことがある。その場合も残量だけは返す。
/// </remarks>
public sealed class BatteryInfoReader : IHardwareReader<IReadOnlyList<BatteryInfo>>
{
    private const string BatteryQuery = "SELECT Name, EstimatedChargeRemaining FROM Win32_Battery";
    private const string DesignCapacityQuery = "SELECT DesignedCapacity FROM BatteryStaticData";
    private const string FullChargeCapacityQuery = "SELECT FullChargedCapacity FROM BatteryFullChargedCapacity";
    private const string CycleCountQuery = "SELECT CycleCount FROM BatteryCycleCount";

    private readonly IWmiQueryExecutor _wmi;

    /// <summary>
    /// 読み取りに使う WMI 実行器を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public BatteryInfoReader(IWmiQueryExecutor wmi)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        _wmi = wmi;
    }

    /// <inheritdoc />
    /// <returns>バッテリーの一覧。バッテリーがない PC では空。</returns>
    public IReadOnlyList<BatteryInfo> Read()
    {
        var batteries = _wmi.Query(WmiNamespaces.Cimv2, BatteryQuery);
        if (batteries.Count == 0)
        {
            return [];
        }

        var design = QueryOptional(DesignCapacityQuery, "DesignedCapacity");
        var full = QueryOptional(FullChargeCapacityQuery, "FullChargedCapacity");
        var cycles = QueryOptional(CycleCountQuery, "CycleCount");

        return batteries
            .Select((r, i) => new BatteryInfo(
                Name: r.GetString("Name") ?? "バッテリー",
                ChargePercent: r.GetInt32("EstimatedChargeRemaining"),
                DesignCapacityMwh: ElementOrNull(design, i),
                FullChargeCapacityMwh: ElementOrNull(full, i))
            {
                CycleCount = ElementOrNull(cycles, i),
            })
            .ToArray();
    }

    private static int? ElementOrNull(IReadOnlyList<int?> values, int index) =>
        index < values.Count ? values[index] : null;

    private IReadOnlyList<int?> QueryOptional(string query, string property) =>
        _wmi.QueryOrEmpty(WmiNamespaces.Wmi, query).Select(r => r.GetInt32(property)).ToArray();
}

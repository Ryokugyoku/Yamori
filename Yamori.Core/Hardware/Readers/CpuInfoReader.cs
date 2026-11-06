using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// Win32_Processor から CPU の情報を読み取る。複数ソケットの場合はコア数を合計する。
/// </summary>
public sealed class CpuInfoReader : IHardwareReader<CpuInfo?>
{
    private const string Query =
        "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor";

    private readonly IWmiQueryExecutor _wmi;

    /// <summary>
    /// 読み取りに使う WMI 実行器を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public CpuInfoReader(IWmiQueryExecutor wmi)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        _wmi = wmi;
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

        return new CpuInfo(
            Name: rows[0].GetString("Name") ?? "不明な CPU",
            PhysicalCores: rows.Sum(r => r.GetInt32("NumberOfCores") ?? 0),
            LogicalProcessors: rows.Sum(r => r.GetInt32("NumberOfLogicalProcessors") ?? 0),
            MaxClockMhz: rows.Max(r => r.GetInt32("MaxClockSpeed") ?? 0));
    }
}

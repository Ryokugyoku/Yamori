using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// Win32_PhysicalMemory から搭載メモリの情報を読み取る。
/// </summary>
public sealed class MemoryInfoReader : IHardwareReader<MemoryInfo?>
{
    private const string Query = "SELECT Capacity, Speed, ConfiguredClockSpeed FROM Win32_PhysicalMemory";

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
        var rows = _wmi.Query(WmiNamespaces.Cimv2, Query);
        if (rows.Count == 0)
        {
            return null;
        }

        var total = rows.Aggregate(0UL, (sum, r) => sum + (r.GetUInt64("Capacity") ?? 0));
        var speed = rows
            .Select(r => r.GetInt32("ConfiguredClockSpeed") ?? r.GetInt32("Speed"))
            .Where(s => s is > 0)
            .DefaultIfEmpty(null)
            .Min();

        return new MemoryInfo(total, rows.Count, speed);
    }
}

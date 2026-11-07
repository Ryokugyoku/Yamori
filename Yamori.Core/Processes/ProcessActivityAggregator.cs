namespace Yamori.Core.Processes;

/// <summary>
/// プロセスごとの CPU・GPU・入出力の使用量を、<see cref="ProcessUsageAggregator"/> と同じ方法（名前が同じプロセスをまとめる）でアプリ単位にする。
/// </summary>
public static class ProcessActivityAggregator
{
    /// <summary>
    /// プロセス名ごとに使用量を合計する。アプリではないもの（Idle、System など）は除く。
    /// </summary>
    /// <param name="activities">プロセスごとの使用量。</param>
    /// <returns>アプリ単位の使用量。順序は決めない。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="activities"/> が null。</exception>
    public static IReadOnlyList<AppActivity> GroupByApp(IEnumerable<ProcessActivity> activities)
    {
        ArgumentNullException.ThrowIfNull(activities);

        return activities
            .Where(a => ProcessNameFilter.IsApplication(a.Name))
            .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new AppActivity(
                g.First().Name,
                g.Count(),
                Math.Clamp(g.Sum(a => a.CpuPercent), 0, 100),
                Math.Clamp(g.Sum(a => a.GpuPercent), 0, 100),
                g.Sum(a => Math.Max(0, a.ReadBytesPerSecond)),
                g.Sum(a => Math.Max(0, a.WriteBytesPerSecond))))
            .ToArray();
    }

    /// <summary>
    /// 指定した値の大きい順にアプリを並べ、上位だけを返す。値が 0 のアプリは含めない。
    /// </summary>
    /// <param name="apps">アプリ単位の使用量。</param>
    /// <param name="selector">並べる基準の値を取り出す処理。</param>
    /// <param name="top">返す件数の上限。</param>
    /// <returns>値の大きい順のアプリ。同じ値なら名前の順。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="apps"/> か <paramref name="selector"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="top"/> が 0 以下。</exception>
    public static IReadOnlyList<AppActivity> Top(IEnumerable<AppActivity> apps, Func<AppActivity, double> selector, int top)
    {
        ArgumentNullException.ThrowIfNull(apps);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        return apps
            .Where(a => selector(a) > 0)
            .OrderByDescending(selector)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
    }
}

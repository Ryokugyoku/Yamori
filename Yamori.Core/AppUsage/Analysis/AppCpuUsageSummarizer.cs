using Yamori.Core.AppUsage.Storage;

namespace Yamori.Core.AppUsage.Analysis;

/// <summary>
/// 記録の合計（<see cref="AppCpuTotals"/>）から、アプリごとの平均の使用率を求めてまとめる。
/// </summary>
public static class AppCpuUsageSummarizer
{
    /// <summary>
    /// 平均を求め、平均の多い上位と、高負荷の時間の平均の多い上位を合わせて返す。
    /// </summary>
    /// <param name="totals">記録の合計。</param>
    /// <param name="highLoadThresholdPercent">高負荷とみなしたしきい値（%）。</param>
    /// <param name="top">それぞれの順位で残す数。</param>
    /// <returns>まとめ。アプリは平均の使用率の大きい順（同じなら名前の順）。記録がなければ <see cref="AppCpuUsageSummary.Empty"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="totals"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="top"/> が 0 以下。</exception>
    public static AppCpuUsageSummary Summarize(AppCpuTotals totals, double highLoadThresholdPercent, int top)
    {
        ArgumentNullException.ThrowIfNull(totals);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        if (totals.ObservedMinutes <= 0)
        {
            return AppCpuUsageSummary.Empty;
        }

        var apps = totals.Apps
            .Select(a => new AppCpuUsage(
                a.Name,
                Math.Clamp(a.CpuPercentSum / totals.ObservedMinutes, 0, 100),
                totals.HighLoadMinutes > 0 ? Math.Clamp(a.HighLoadCpuPercentSum / totals.HighLoadMinutes, 0, 100) : 0))
            .ToArray();

        var byAverage = apps
            .OrderByDescending(a => a.AveragePercent)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Take(top);
        var byHighLoad = apps
            .Where(a => a.HighLoadAveragePercent > 0)
            .OrderByDescending(a => a.HighLoadAveragePercent)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Take(top);

        var selected = byAverage
            .Union(byHighLoad)
            .OrderByDescending(a => a.AveragePercent)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AppCpuUsageSummary(
            TimeSpan.FromMinutes(totals.ObservedMinutes),
            TimeSpan.FromMinutes(totals.HighLoadMinutes),
            highLoadThresholdPercent,
            selected);
    }
}

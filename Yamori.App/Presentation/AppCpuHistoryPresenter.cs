using System;
using System.Collections.Generic;
using System.Linq;
using Yamori.Core.AppUsage.Analysis;
using Yamori.Core.Formatting;
using Yamori.Localization;

namespace Yamori.Presentation;

/// <summary>
/// アプリごとの CPU 使用率の記録のまとめを、アプリ画面の表示内容にする。
/// </summary>
public sealed class AppCpuHistoryPresenter
{
    /// <summary>
    /// 平均の多い順の一覧を作る。
    /// </summary>
    /// <param name="summary">記録のまとめ。</param>
    /// <param name="count">出す最大の数。</param>
    /// <returns>表示内容。記録がなければ空。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="summary"/> が null。</exception>
    public IReadOnlyList<AppCpuHistoryItem> BuildItems(AppCpuUsageSummary summary, int count)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var apps = summary.Apps
            .Where(a => a.AveragePercent > 0)
            .OrderByDescending(a => a.AveragePercent)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .ToArray();
        if (apps.Length == 0)
        {
            return [];
        }

        var max = apps[0].AveragePercent;
        var hasHighLoad = summary.HighLoad > TimeSpan.Zero;
        return apps
            .Select((a, i) => new AppCpuHistoryItem(
                i + 1,
                a.Name,
                UiText.Format("Apps_CpuAverage", a.AveragePercent),
                hasHighLoad ? UiText.Format("Apps_CpuHighLoad", a.HighLoadAveragePercent) : string.Empty,
                max > 0 ? a.AveragePercent / max * 100 : 0))
            .ToArray();
    }

    /// <summary>
    /// 記録した時間と、そのうち高負荷だった時間の説明を作る。
    /// </summary>
    /// <param name="summary">記録のまとめ。</param>
    /// <returns>説明。記録がなければ、記録がない理由の説明。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="summary"/> が null。</exception>
    public string BuildSummaryText(AppCpuUsageSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return summary.HasData
            ? UiText.Format(
                "Apps_CpuHistorySummary",
                DurationFormatter.Format(summary.Observed),
                summary.HighLoadThresholdPercent,
                DurationFormatter.Format(summary.HighLoad))
            : UiText.Get("Apps_CpuHistoryNoData");
    }
}

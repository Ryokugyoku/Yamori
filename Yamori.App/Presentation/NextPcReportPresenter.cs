using System;
using System.Collections.Generic;
using System.Linq;
using Yamori.Core.Formatting;
using Yamori.Core.Hardware;
using Yamori.Core.Processes;
using Yamori.Core.Recommendations;
using Yamori.Localization;

namespace Yamori.Presentation;

/// <summary>
/// <see cref="Core.Reports.NextPcReport"/> の中身を、画面に出す項目の一覧に変換する。言語は現在の UI カルチャに従う。
/// </summary>
public sealed class NextPcReportPresenter
{
    private readonly RecommendationTextFormatter _recommendationFormatter;

    /// <summary>
    /// おすすめの文章化処理を指定して作る。
    /// </summary>
    /// <param name="recommendationFormatter">おすすめを文章にする処理。</param>
    /// <exception cref="ArgumentNullException"><paramref name="recommendationFormatter"/> が null。</exception>
    public NextPcReportPresenter(RecommendationTextFormatter recommendationFormatter)
    {
        ArgumentNullException.ThrowIfNull(recommendationFormatter);
        _recommendationFormatter = recommendationFormatter;
    }

    /// <summary>
    /// おすすめを表示用の項目にする。
    /// </summary>
    /// <param name="recommendations">おすすめの一覧。</param>
    /// <returns>表示用の項目。</returns>
    public IReadOnlyList<RecommendationItem> BuildRecommendations(IEnumerable<HardwareRecommendation> recommendations)
    {
        ArgumentNullException.ThrowIfNull(recommendations);
        return recommendations
            .Select(r => (Recommendation: r, Text: _recommendationFormatter.Format(r)))
            .Select(x => new RecommendationItem(
                HardwareGlyphs.For(x.Recommendation.Component),
                x.Text.Title,
                x.Text.Explanation,
                LevelOf(x.Recommendation.Level),
                UiText.Get("RecLevel_" + x.Recommendation.Level)))
            .ToArray();
    }

    /// <summary>
    /// 判定を札の色にする。足りないときも赤にはしない（今すぐ困るわけではなく、次の PC を選ぶときの目安のため）。
    /// </summary>
    private static StatusLevel LevelOf(RecommendationLevel level) =>
        level switch
        {
            RecommendationLevel.Sufficient => StatusLevel.Good,
            RecommendationLevel.Borderline => StatusLevel.Attention,
            RecommendationLevel.Insufficient => StatusLevel.Caution,
            _ => StatusLevel.Neutral,
        };

    /// <summary>
    /// アプリの使用量を表示用の項目にする。
    /// </summary>
    /// <param name="apps">アプリ単位の使用量。</param>
    /// <returns>表示用の項目。</returns>
    public IReadOnlyList<AppUsageItem> BuildTopApps(IEnumerable<ProcessUsage> apps)
    {
        ArgumentNullException.ThrowIfNull(apps);

        var list = apps.ToArray();
        var largest = list.Length == 0 ? 0UL : list.Max(a => a.WorkingSetBytes);
        return list
            .Select((a, index) => new AppUsageItem(
                index + 1,
                a.Name,
                ByteSizeFormatter.Format(a.WorkingSetBytes),
                UiText.Format("Apps_ProcessCount", a.ProcessCount),
                largest == 0 ? 0 : 100.0 * a.WorkingSetBytes / largest))
            .ToArray();
    }
}

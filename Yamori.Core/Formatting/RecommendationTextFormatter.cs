using Yamori.Core.Hardware;
using Yamori.Core.Localization;
using Yamori.Core.Recommendations;

namespace Yamori.Core.Formatting;

/// <summary>
/// <see cref="HardwareRecommendation"/> を、事実と根拠を添えた文章にする。言語は現在の UI カルチャに従う。
/// </summary>
/// <remarks>
/// 方針：不安をあおらない。測った事実、その意味、次の PC での目安だけを書く。
/// </remarks>
public sealed class RecommendationTextFormatter
{
    /// <summary>
    /// おすすめを文章にする。
    /// </summary>
    /// <param name="recommendation">文章にするおすすめ。</param>
    /// <returns>見出しと説明。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recommendation"/> が null。</exception>
    public RecommendationText Format(HardwareRecommendation recommendation)
    {
        ArgumentNullException.ThrowIfNull(recommendation);

        var name = ComponentDisplayNames.Get(recommendation.Component);
        if (recommendation.Level == RecommendationLevel.NotEnoughData)
        {
            return new RecommendationText(
                CoreText.Format("Rec_NotEnoughData_Title", name),
                CoreText.Format("Rec_NotEnoughData_Explanation", FormatDuration(recommendation.HighLoad.TotalObserved)));
        }

        var fact = CoreText.Format(
            "Rec_Fact",
            name,
            recommendation.HighLoad.ThresholdPercent,
            FormatDuration(recommendation.HighLoad.TotalObserved),
            recommendation.HighLoad.Ratio * 100);

        var causes = recommendation.MainCauses.Count > 0
            ? CoreText.Format("Rec_Causes", string.Join(CoreText.Get("Rec_CauseSeparator"), recommendation.MainCauses))
            : string.Empty;

        var meaning = recommendation.Level switch
        {
            RecommendationLevel.Sufficient => CoreText.Get("Rec_Meaning_Sufficient"),
            RecommendationLevel.Borderline => CoreText.Get("Rec_Meaning_Borderline"),
            RecommendationLevel.Insufficient => CoreText.Get("Rec_Meaning_Insufficient"),
            _ => string.Empty,
        };

        return new RecommendationText(
            FormatTitle(name, recommendation),
            string.Concat(fact, causes, meaning));
    }

    private static string FormatTitle(string name, HardwareRecommendation recommendation)
    {
        if (recommendation.RecommendedAmount is not { } amount)
        {
            return CoreText.Format("Rec_Title_Level", name, LevelLabel(recommendation.Level));
        }

        var unit = recommendation.Component == HardwareComponent.Memory
            ? CoreText.Format("Unit_Gigabytes", amount)
            : CoreText.Format("Unit_Threads", amount);

        return recommendation.Level == RecommendationLevel.Sufficient
            ? CoreText.Format("Rec_Title_Sufficient", name, unit)
            : CoreText.Format("Rec_Title_Recommended", name, unit);
    }

    private static string LevelLabel(RecommendationLevel level) =>
        level switch
        {
            RecommendationLevel.Sufficient => CoreText.Get("Level_Sufficient"),
            RecommendationLevel.Borderline => CoreText.Get("Level_Borderline"),
            RecommendationLevel.Insufficient => CoreText.Get("Level_Insufficient"),
            _ => CoreText.Get("Level_Unknown"),
        };

    private static string FormatDuration(TimeSpan duration) => DurationFormatter.Format(duration);
}

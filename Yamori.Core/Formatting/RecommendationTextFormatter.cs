using System.Globalization;
using Yamori.Core.Hardware;
using Yamori.Core.Recommendations;

namespace Yamori.Core.Formatting;

/// <summary>
/// <see cref="HardwareRecommendation"/> を、事実と根拠を添えた日本語の文章にする。
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
                $"{name}：データを集めています",
                $"判定には、もう少し長い時間の記録が必要です（今の記録：{FormatDuration(recommendation.HighLoad.TotalObserved)}）。"
                + "Yamori を開いている間、使用率を記録しています。");
        }

        var fact = string.Format(
            CultureInfo.InvariantCulture,
            "{0}使用率が{1:0}%を超えていた時間は、記録した時間（{2}）全体の{3:0}%です。",
            name,
            recommendation.HighLoad.ThresholdPercent,
            FormatDuration(recommendation.HighLoad.TotalObserved),
            recommendation.HighLoad.Ratio * 100);

        var causes = recommendation.MainCauses.Count > 0
            ? $"主な原因は{string.Join("と", recommendation.MainCauses)}です。"
            : string.Empty;

        var meaning = recommendation.Level switch
        {
            RecommendationLevel.Sufficient => "今の使い方には足りています。",
            RecommendationLevel.Borderline => "ときどき限界に近づいていますが、普段の作業に大きな支障はない水準です。",
            RecommendationLevel.Insufficient => "限界に近い状態で使っている時間が長く、動作が遅くなる原因になっている可能性があります。",
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
            return $"{name}：{LevelLabel(recommendation.Level)}";
        }

        var unit = recommendation.Component == HardwareComponent.Memory
            ? $"{amount}GB"
            : $"{amount}スレッド";

        return recommendation.Level == RecommendationLevel.Sufficient
            ? $"{name}：次の PC も {unit} あれば十分です"
            : $"{name}：次の PC は {unit} 以上がおすすめです";
    }

    private static string LevelLabel(RecommendationLevel level) =>
        level switch
        {
            RecommendationLevel.Sufficient => "足りています",
            RecommendationLevel.Borderline => "ときどき限界に近い",
            RecommendationLevel.Insufficient => "限界に近い時間が長い",
            _ => "判定できません",
        };

    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalDays >= 1
            ? string.Format(CultureInfo.InvariantCulture, "{0:0.#}日", duration.TotalDays)
            : duration.TotalHours >= 1
                ? string.Format(CultureInfo.InvariantCulture, "{0:0.#}時間", duration.TotalHours)
                : string.Format(CultureInfo.InvariantCulture, "{0:0}分", duration.TotalMinutes);
}

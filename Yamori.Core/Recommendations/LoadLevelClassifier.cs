using Yamori.Core.Analysis;

namespace Yamori.Core.Recommendations;

/// <summary>
/// 高負荷の時間の割合から、<see cref="RecommendationLevel"/> を決める。
/// </summary>
public static class LoadLevelClassifier
{
    /// <summary>
    /// 高負荷の時間の割合を判定する。
    /// </summary>
    /// <param name="highLoad">高負荷しきい値での集計結果。</param>
    /// <param name="options">判定の基準値。</param>
    /// <returns>判定結果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> が null。</exception>
    public static RecommendationLevel Classify(ThresholdExposure highLoad, RecommendationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (highLoad.TotalObserved < options.MinimumObservation)
        {
            return RecommendationLevel.NotEnoughData;
        }

        return highLoad.Ratio switch
        {
            var r when r >= options.InsufficientRatio => RecommendationLevel.Insufficient,
            var r when r >= options.BorderlineRatio => RecommendationLevel.Borderline,
            _ => RecommendationLevel.Sufficient,
        };
    }
}

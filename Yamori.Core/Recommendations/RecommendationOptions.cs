namespace Yamori.Core.Recommendations;

/// <summary>
/// おすすめを判定するときの基準値。
/// </summary>
public sealed record RecommendationOptions
{
    /// <summary>
    /// 既定の基準値。
    /// </summary>
    public static RecommendationOptions Default { get; } = new();

    /// <summary>
    /// 「高負荷」とみなす使用率（%）を取得する。既定は 80。
    /// </summary>
    public double HighLoadThresholdPercent { get; init; } = 80;

    /// <summary>
    /// 高負荷の時間の割合がこれ以上なら <see cref="RecommendationLevel.Borderline"/> とする。既定は 0.05（5%）。
    /// </summary>
    public double BorderlineRatio { get; init; } = 0.05;

    /// <summary>
    /// 高負荷の時間の割合がこれ以上なら <see cref="RecommendationLevel.Insufficient"/> とする。既定は 0.2（20%）。
    /// </summary>
    public double InsufficientRatio { get; init; } = 0.2;

    /// <summary>
    /// 判定に必要な最低限の測定時間を取得する。これより短いと <see cref="RecommendationLevel.NotEnoughData"/> とする。既定は 1 時間。
    /// </summary>
    public TimeSpan MinimumObservation { get; init; } = TimeSpan.FromHours(1);
}

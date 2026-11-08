namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 範囲の中の 1 本の値の集計値。
/// </summary>
/// <param name="Minimum">最小値。</param>
/// <param name="Average">平均値（点が表す時間の長さで重みをつける）。</param>
/// <param name="Maximum">最大値。</param>
/// <param name="Percentile95">95 パーセンタイル（95% の時間はこの値以下だった）。</param>
/// <param name="Observed">記録があった時間の合計。</param>
/// <param name="AboveThreshold">しきい値以上だった時間の合計。しきい値を渡さなかった場合は null。</param>
public sealed record SeriesStatistics(
    double Minimum,
    double Average,
    double Maximum,
    double Percentile95,
    TimeSpan Observed,
    TimeSpan? AboveThreshold);

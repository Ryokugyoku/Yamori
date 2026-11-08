using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 記録から集計値（最小・平均・最大・95%・しきい値以上の時間）を求める。点ごとに、その点が表す時間の長さで重みをつける
/// （1 秒ごとの記録と 1 分集計がまざっても、時間の割合どおりに数える）。
/// </summary>
/// <remarks>
/// 平均・95%・「しきい値以上の時間」は点ごとの平均で数える（1 秒ごとの記録では毎秒の値、1 分集計では 1 分の平均）。
/// 1 分集計では 1 分より短い山がならされるため、1 秒ごとの記録より少なめに出る。最小・最大は集計の最小・最大から取る。
/// </remarks>
public static class SeriesStatisticsCalculator
{
    /// <summary>パーセンタイルの割合。</summary>
    private const double PercentileRatio = 0.95;

    /// <summary>
    /// 同じ解像度の記録から集計値を求める。
    /// </summary>
    /// <param name="points">記録。</param>
    /// <param name="step">1 点が表す時間の長さ。</param>
    /// <param name="threshold">この値以上の時間を数えるしきい値。数えないなら null。</param>
    /// <returns>集計値。使える点がなければ null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="points"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="step"/> が 0 以下。</exception>
    public static SeriesStatistics? Calculate(IReadOnlyList<SensorHistoryPoint> points, TimeSpan step, double? threshold)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(step, TimeSpan.Zero);
        return Calculate(points.Select(p => (p, step)), threshold);
    }

    /// <summary>
    /// 解像度のまざった記録から集計値を求める。
    /// </summary>
    /// <param name="pieces">記録のまとまり。</param>
    /// <param name="threshold">この値以上の時間を数えるしきい値。数えないなら null。</param>
    /// <returns>集計値。使える点がなければ null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pieces"/> が null。</exception>
    public static SeriesStatistics? Calculate(IReadOnlyList<HistoryPiece> pieces, double? threshold)
    {
        ArgumentNullException.ThrowIfNull(pieces);
        return Calculate(pieces.SelectMany(piece => piece.Points.Select(p => (p, piece.Step))), threshold);
    }

    private static SeriesStatistics? Calculate(IEnumerable<(SensorHistoryPoint Point, TimeSpan Step)> source, double? threshold)
    {
        var usable = source.Where(s => IsUsable(s.Point)).ToArray();
        if (usable.Length == 0)
        {
            return null;
        }

        var observed = TimeSpan.Zero;
        var above = TimeSpan.Zero;
        var sum = 0.0;
        foreach (var (point, step) in usable)
        {
            observed += step;
            sum += point.Average * step.TotalSeconds;
            if (threshold is { } t && point.Average >= t)
            {
                above += step;
            }
        }

        return new SeriesStatistics(
            usable.Min(s => s.Point.Min),
            sum / observed.TotalSeconds,
            usable.Max(s => s.Point.Max),
            Percentile(usable, observed),
            observed,
            threshold is null ? null : above);
    }

    /// <summary>
    /// 時間で重みをつけた 95 パーセンタイルを求める（値の小さい順に時間を足し、全体の 95% に届いたところの値）。
    /// </summary>
    private static double Percentile(IReadOnlyList<(SensorHistoryPoint Point, TimeSpan Step)> usable, TimeSpan observed)
    {
        var target = observed.Ticks * PercentileRatio;
        var accumulated = 0L;
        foreach (var (point, step) in usable.OrderBy(s => s.Point.Average))
        {
            accumulated += step.Ticks;
            if (accumulated >= target)
            {
                return point.Average;
            }
        }

        return usable.Max(s => s.Point.Average);
    }

    private static bool IsUsable(SensorHistoryPoint point) =>
        double.IsFinite(point.Average) && double.IsFinite(point.Min) && double.IsFinite(point.Max);
}

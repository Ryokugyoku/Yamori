using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// グラフに描く点の数を減らす。範囲を同じ長さの区切りに分け、区切りごとに最小・最大・平均をまとめる
/// （最小・最大を残すので、数秒の山もグラフから消えない）。
/// </summary>
public static class SeriesDownsampler
{
    /// <summary>
    /// 点の数を減らす。点が区切りの数以下なら、そのまま返す。
    /// </summary>
    /// <param name="points">記録（古い順）。</param>
    /// <param name="range">グラフの範囲。</param>
    /// <param name="bucketCount">区切りの数。</param>
    /// <returns>区切りごとの点（区切りの始まりの時刻。点のない区切りは含めない）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="points"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bucketCount"/> が 0 以下。</exception>
    public static IReadOnlyList<SensorHistoryPoint> Downsample(IReadOnlyList<SensorHistoryPoint> points, AnalysisRange range, int bucketCount)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bucketCount);

        if (points.Count <= bucketCount || range.Length <= TimeSpan.Zero)
        {
            return points;
        }

        var bucketTicks = Math.Max(1, range.Length.Ticks / bucketCount);
        var result = new List<SensorHistoryPoint>(bucketCount);
        long? current = null;
        double min = 0, max = 0, sum = 0;
        uint count = 0;
        var weight = 0.0;

        void Flush()
        {
            if (current is { } bucket && weight > 0)
            {
                result.Add(new SensorHistoryPoint(range.FromUtc + TimeSpan.FromTicks(bucket * bucketTicks), min, max, sum / weight, count));
            }
        }

        foreach (var point in points)
        {
            if (!double.IsFinite(point.Average) || point.TimestampUtc < range.FromUtc || point.TimestampUtc >= range.ToUtc)
            {
                continue;
            }

            var index = (point.TimestampUtc - range.FromUtc).Ticks / bucketTicks;
            if (index != current)
            {
                Flush();
                current = index;
                min = point.Min;
                max = point.Max;
                sum = 0;
                count = 0;
                weight = 0;
            }

            var w = Math.Max(1u, point.Count);
            min = Math.Min(min, point.Min);
            max = Math.Max(max, point.Max);
            sum += point.Average * w;
            weight += w;
            count += point.Count;
        }

        Flush();
        return result;
    }
}

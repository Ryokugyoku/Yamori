namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 生データや細かい集計を、区間ごとの min・max・avg・count にまとめる。
/// </summary>
public static class RollupCalculator
{
    /// <summary>
    /// 生データを区間ごとに集計する。
    /// </summary>
    /// <param name="points">1 つのセンサーの生データ。</param>
    /// <param name="bucketMs">区間の長さ（ミリ秒）。</param>
    /// <returns>点のあった区間の集計を、区間の順に並べたもの。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="points"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bucketMs"/> が 0 以下。</exception>
    public static IReadOnlyList<RollupPoint> FromRaw(IEnumerable<TimeSeriesPoint> points, long bucketMs)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bucketMs);

        return points
            .GroupBy(p => (p.SensorId, Bucket: TimeSeriesResolutions.FloorToBucket(p.TimestampMs, bucketMs)))
            .Select(g => new RollupPoint(
                g.Key.SensorId,
                g.Key.Bucket,
                g.Min(p => p.Value),
                g.Max(p => p.Value),
                g.Average(p => p.Value),
                (uint)g.Count()))
            .OrderBy(r => r.SensorId)
            .ThenBy(r => r.BucketStartMs)
            .ToArray();
    }

    /// <summary>
    /// 細かい集計を、より長い区間にまとめる。平均は点の数で重み付けする。
    /// </summary>
    /// <param name="rollups">1 つのセンサーの細かい集計。</param>
    /// <param name="bucketMs">まとめる区間の長さ（ミリ秒）。</param>
    /// <returns>まとめた集計を、区間の順に並べたもの。点の数が 0 の区間は含めない。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rollups"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bucketMs"/> が 0 以下。</exception>
    public static IReadOnlyList<RollupPoint> Combine(IEnumerable<RollupPoint> rollups, long bucketMs)
    {
        ArgumentNullException.ThrowIfNull(rollups);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bucketMs);

        return rollups
            .Where(r => r.Count > 0)
            .GroupBy(r => (r.SensorId, Bucket: TimeSeriesResolutions.FloorToBucket(r.BucketStartMs, bucketMs)))
            .Select(g =>
            {
                var count = g.Aggregate(0UL, (sum, r) => sum + r.Count);
                return new RollupPoint(
                    g.Key.SensorId,
                    g.Key.Bucket,
                    g.Min(r => r.Min),
                    g.Max(r => r.Max),
                    g.Sum(r => r.Average * r.Count) / count,
                    (uint)Math.Min(count, uint.MaxValue));
            })
            .OrderBy(r => r.SensorId)
            .ThenBy(r => r.BucketStartMs)
            .ToArray();
    }
}

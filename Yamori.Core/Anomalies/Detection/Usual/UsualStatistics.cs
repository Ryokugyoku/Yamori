namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 「ふだんより悪い」の判定で使う、中央値と百分位の計算と、時刻の区切り。
/// </summary>
public static class UsualStatistics
{
    /// <summary>
    /// 中央値を求める。
    /// </summary>
    /// <param name="values">値。</param>
    /// <returns>中央値。値がなければ null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> が null。</exception>
    public static double? Median(IEnumerable<double> values) => Percentile(values, 50);

    /// <summary>
    /// 百分位を、隣り合う値の間を直線で補って求める。
    /// </summary>
    /// <param name="values">値。</param>
    /// <param name="percentile">百分位（0〜100）。</param>
    /// <returns>百分位の値。値がなければ null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="percentile"/> が 0〜100 の外。</exception>
    public static double? Percentile(IEnumerable<double> values, double percentile)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfLessThan(percentile, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentile, 100);

        var sorted = values.Order().ToArray();
        if (sorted.Length == 0)
        {
            return null;
        }

        var position = (sorted.Length - 1) * percentile / 100;
        var lower = (int)Math.Floor(position);
        var upper = Math.Min(lower + 1, sorted.Length - 1);
        return sorted[lower] + ((sorted[upper] - sorted[lower]) * (position - lower));
    }

    /// <summary>
    /// 時刻を、区切りの長さの倍数（UTC の 0 時起点）に切り下げる。
    /// </summary>
    /// <param name="timestampUtc">時刻。</param>
    /// <param name="length">区切りの長さ。</param>
    /// <returns>区切りの始まり。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> が 0 以下。</exception>
    public static DateTimeOffset FloorTo(DateTimeOffset timestampUtc, TimeSpan length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, TimeSpan.Zero);
        var ticks = timestampUtc.UtcTicks;
        return new DateTimeOffset(ticks - (ticks % length.Ticks), TimeSpan.Zero);
    }
}

namespace Yamori.Core.Analysis;

/// <summary>
/// 負荷時間の分析。一時的な負荷の山ではなく、しきい値を超えていた「時間の割合」を求める。
/// </summary>
public static class LoadAnalyzer
{
    /// <summary>
    /// 既定の上限。これより長いサンプル間隔は、PCが止まっていた（スリープ・電源オフ）とみなして集計に入れない。
    /// </summary>
    public static readonly TimeSpan DefaultMaxGap = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 各サンプルの値を、次のサンプルまでの間ずっと続いていたとみなして、しきい値を超えていた時間を集計する。
    /// </summary>
    /// <param name="samples">使用率のサンプル。順序は問わない。</param>
    /// <param name="thresholds">しきい値（例：50, 80）。</param>
    /// <param name="maxGap">これより長い間隔は集計に入れない。省略時は <see cref="DefaultMaxGap"/>。</param>
    /// <returns><paramref name="thresholds"/> と同じ順序の、しきい値ごとの集計結果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="samples"/> または <paramref name="thresholds"/> が null。</exception>
    public static IReadOnlyList<ThresholdExposure> ComputeExposure(
        IEnumerable<UsageSample> samples,
        IEnumerable<double> thresholds,
        TimeSpan? maxGap = null)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(thresholds);

        var gapLimit = maxGap ?? DefaultMaxGap;
        var ordered = samples.OrderBy(s => s.Timestamp).ToArray();
        var limits = thresholds.ToArray();
        var above = new TimeSpan[limits.Length];
        var total = TimeSpan.Zero;

        for (var i = 0; i < ordered.Length - 1; i++)
        {
            var span = ordered[i + 1].Timestamp - ordered[i].Timestamp;
            if (span <= TimeSpan.Zero || span > gapLimit)
            {
                continue;
            }

            total += span;
            for (var t = 0; t < limits.Length; t++)
            {
                if (ordered[i].UsagePercent > limits[t])
                {
                    above[t] += span;
                }
            }
        }

        var result = new ThresholdExposure[limits.Length];
        for (var t = 0; t < limits.Length; t++)
        {
            result[t] = new ThresholdExposure(limits[t], above[t], total);
        }

        return result;
    }
}

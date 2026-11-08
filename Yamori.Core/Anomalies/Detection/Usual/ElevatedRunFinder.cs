namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 直近の区切りから遡って、ふだんより高い状態が今も続いている期間を探す。短い山（続いた時間の下限に満たないもの）は無視する。
/// </summary>
/// <remarks>
/// 判定できない区切り（待機中の時間がない、記録がない）は数えず、<see cref="UsualComparisonOptions.MaxGap"/> までなら挟んでも続きとみなす。
/// 判定できる区切りでしきい値を下回ったら、そこで切る。
/// </remarks>
public static class ElevatedRunFinder
{
    /// <summary>
    /// 今も続いている「高い」期間を探す。
    /// </summary>
    /// <param name="buckets">判定する直近の期間の区切り（古い順、抜けなく並べたもの）。</param>
    /// <param name="select">区切りから比べる値を選ぶ。</param>
    /// <param name="threshold">ふだんより高いとみなす差。</param>
    /// <param name="options">決まりごと。</param>
    /// <returns>続いている期間。最も新しい判定できる区切りが高くないか、続いた時間の下限に満たなければ null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="threshold"/> 以外の引数が null。</exception>
    public static ElevatedRun? Find(
        IReadOnlyList<UsualBucketResult> buckets,
        Func<UsualBucketResult, UsualMetricValue?> select,
        double threshold,
        UsualComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(buckets);
        ArgumentNullException.ThrowIfNull(select);
        ArgumentNullException.ThrowIfNull(options);

        var index = buckets.Count - 1;
        var gap = TimeSpan.Zero;
        while (index >= 0 && select(buckets[index]) is null)
        {
            gap += options.BucketLength;
            index--;
        }

        if (index < 0 || gap > options.MaxGap || select(buckets[index])!.Value.Delta < threshold)
        {
            return null;
        }

        var end = buckets[index].StartUtc + options.BucketLength;
        var start = end;
        double values = 0, baselines = 0;
        var count = 0;
        gap = TimeSpan.Zero;
        for (; index >= 0; index--)
        {
            if (select(buckets[index]) is not { } metric)
            {
                gap += options.BucketLength;
                if (gap > options.MaxGap)
                {
                    break;
                }

                continue;
            }

            if (metric.Delta < threshold)
            {
                break;
            }

            gap = TimeSpan.Zero;
            count++;
            start = buckets[index].StartUtc;
            values += metric.Value;
            baselines += metric.Baseline;
        }

        var elevated = options.BucketLength * count;
        return elevated >= options.MinimumElevatedDuration
            ? new ElevatedRun(start, end, elevated, values / count, baselines / count)
            : null;
    }
}

namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 比べる相手の期間から求めた「ふだん」。負荷の帯ごとの待機中の温度の中央値と、区切りごとの待機中の負荷の中央値。
/// </summary>
public sealed class UsualBaseline
{
    private UsualBaseline(IReadOnlyDictionary<int, double> temperatureByBand, double? idleLoadPercent)
    {
        TemperatureByBand = temperatureByBand;
        IdleLoadPercent = idleLoadPercent;
    }

    /// <summary>負荷の帯ごとの、ふだんの待機中の温度（中央値、℃）を取得する。分の数が足りない帯は含まない。</summary>
    public IReadOnlyDictionary<int, double> TemperatureByBand { get; }

    /// <summary>ふだんの待機中の負荷（区切りごとの待機中の負荷の中央値、%）を取得する。区切りの数が足りなければ null。</summary>
    public double? IdleLoadPercent { get; }

    /// <summary>
    /// 比べる相手の期間の記録から求める。
    /// </summary>
    /// <param name="minutes">比べる相手の期間の 1 分ごとの記録。</param>
    /// <param name="options">決まりごと。</param>
    /// <returns>ふだん。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static UsualBaseline Build(IReadOnlyList<UsualMinute> minutes, UsualComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(minutes);
        ArgumentNullException.ThrowIfNull(options);

        var temperatures = minutes
            .Where(m => m.TemperatureCelsius is not null)
            .Select(m => (Band: LoadBands.Of(m.LoadPercent, options), Temperature: m.TemperatureCelsius!.Value))
            .Where(x => x.Band is not null)
            .GroupBy(x => x.Band!.Value)
            .Where(g => g.Count() >= options.MinimumBaselineMinutesPerBand)
            .ToDictionary(g => g.Key, g => UsualStatistics.Median(g.Select(x => x.Temperature))!.Value);

        var floors = minutes
            .GroupBy(m => UsualStatistics.FloorTo(m.TimestampUtc, options.BucketLength))
            .Select(g => IdleLoadOf(g.ToArray(), options))
            .OfType<double>()
            .ToArray();
        var idleLoad = floors.Length >= options.MinimumBaselineBuckets ? UsualStatistics.Median(floors) : null;

        return new UsualBaseline(temperatures, idleLoad);
    }

    /// <summary>
    /// 区切り 1 つの「待機中の負荷」（分ごとの負荷の低いほうの百分位）を求める。いちばん静かな時間の負荷で、裏で動き続けるアプリがあると上がる。
    /// </summary>
    /// <param name="bucketMinutes">区切りの中の 1 分ごとの記録。</param>
    /// <param name="options">決まりごと。</param>
    /// <returns>待機中の負荷（%）。記録のある分が足りなければ null。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static double? IdleLoadOf(IReadOnlyList<UsualMinute> bucketMinutes, UsualComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(bucketMinutes);
        ArgumentNullException.ThrowIfNull(options);
        return bucketMinutes.Count >= options.MinimumMinutesPerBucket
            ? UsualStatistics.Percentile(bucketMinutes.Select(m => m.LoadPercent), options.IdleLoadPercentile)
            : null;
    }
}

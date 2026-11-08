namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 区切り 1 つの記録を、ふだんと比べる。温度は同じ負荷の帯の中で比べ（帯ごとの中央値の差を分の数で重みをつけて平均）、
/// 待機中の負荷は区切りのいちばん静かな時間の負荷をふだんと比べる。
/// </summary>
public static class UsualBucketEvaluator
{
    /// <summary>
    /// 区切り 1 つを比べる。
    /// </summary>
    /// <param name="startUtc">区切りの始まり。</param>
    /// <param name="bucketMinutes">区切りの中の 1 分ごとの記録。</param>
    /// <param name="baseline">ふだん。</param>
    /// <param name="options">決まりごと。</param>
    /// <returns>判定の材料。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="startUtc"/> 以外の引数が null。</exception>
    public static UsualBucketResult Evaluate(
        DateTimeOffset startUtc,
        IReadOnlyList<UsualMinute> bucketMinutes,
        UsualBaseline baseline,
        UsualComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(bucketMinutes);
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(options);

        return new UsualBucketResult(startUtc, TemperatureOf(bucketMinutes, baseline, options), IdleLoadOf(bucketMinutes, baseline, options));
    }

    private static UsualMetricValue? TemperatureOf(IReadOnlyList<UsualMinute> minutes, UsualBaseline baseline, UsualComparisonOptions options)
    {
        double weighted = 0, weightedBaseline = 0;
        var count = 0;
        var bands = minutes
            .Where(m => m.TemperatureCelsius is not null)
            .Select(m => (Band: LoadBands.Of(m.LoadPercent, options), Temperature: m.TemperatureCelsius!.Value))
            .Where(x => x.Band is { } band && baseline.TemperatureByBand.ContainsKey(band))
            .GroupBy(x => x.Band!.Value);
        foreach (var band in bands)
        {
            var n = band.Count();
            weighted += UsualStatistics.Median(band.Select(x => x.Temperature))!.Value * n;
            weightedBaseline += baseline.TemperatureByBand[band.Key] * n;
            count += n;
        }

        return count >= options.MinimumIdleMinutesPerBucket && count > 0
            ? new UsualMetricValue(weighted / count, weightedBaseline / count)
            : null;
    }

    private static UsualMetricValue? IdleLoadOf(IReadOnlyList<UsualMinute> minutes, UsualBaseline baseline, UsualComparisonOptions options) =>
        baseline.IdleLoadPercent is { } usual && UsualBaseline.IdleLoadOf(minutes, options) is { } idle
            ? new UsualMetricValue(idle, usual)
            : null;
}

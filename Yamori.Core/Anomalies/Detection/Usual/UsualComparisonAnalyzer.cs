namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 部品 1 つの 1 分ごとの記録を、自分の過去（ふだん）と比べる。直近の期間（<see cref="UsualComparisonOptions.RecentWindow"/>）を区切りごとに判定し、
/// それより前の比べる相手の期間からふだんを求める。
/// </summary>
public static class UsualComparisonAnalyzer
{
    /// <summary>
    /// 読む記録の範囲を求める。
    /// </summary>
    /// <param name="nowUtc">判定する時刻。</param>
    /// <param name="options">決まりごと。</param>
    /// <returns>始まり（含む）と終わり（含まない。最後の終わった区切りの終わり）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> が null。</exception>
    public static (DateTimeOffset FromUtc, DateTimeOffset ToUtc) RangeFor(DateTimeOffset nowUtc, UsualComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return (nowUtc - options.HistoryWindow, UsualStatistics.FloorTo(nowUtc, options.BucketLength));
    }

    /// <summary>
    /// ふだんと比べる。
    /// </summary>
    /// <param name="minutes">1 分ごとの記録（古い順。<see cref="RangeFor"/> の範囲）。</param>
    /// <param name="nowUtc">判定する時刻。</param>
    /// <param name="temperatureThreshold">温度がふだんより何℃ 高ければ「高い」とするか。</param>
    /// <param name="compareIdleLoad">待機中の負荷も比べるなら true。</param>
    /// <param name="options">決まりごと。</param>
    /// <returns>結果。最も古い記録が <see cref="UsualComparisonOptions.MinimumHistory"/> より新しければ、判定しない。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="minutes"/> か <paramref name="options"/> が null。</exception>
    public static UsualComparisonResult Analyze(
        IReadOnlyList<UsualMinute> minutes,
        DateTimeOffset nowUtc,
        double temperatureThreshold,
        bool compareIdleLoad,
        UsualComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(minutes);
        ArgumentNullException.ThrowIfNull(options);

        if (minutes.Count == 0 || minutes.Min(m => m.TimestampUtc) > nowUtc - options.MinimumHistory)
        {
            return UsualComparisonResult.NotEnoughHistory;
        }

        var (_, recentEnd) = RangeFor(nowUtc, options);
        var recentStart = recentEnd - options.RecentWindow;
        var baseline = UsualBaseline.Build(minutes.Where(m => m.TimestampUtc < recentStart).ToArray(), options);

        var byBucket = minutes
            .Where(m => m.TimestampUtc >= recentStart && m.TimestampUtc < recentEnd)
            .GroupBy(m => UsualStatistics.FloorTo(m.TimestampUtc, options.BucketLength))
            .ToDictionary(g => g.Key, g => (IReadOnlyList<UsualMinute>)g.ToArray());
        var buckets = new List<UsualBucketResult>();
        for (var start = UsualStatistics.FloorTo(recentStart, options.BucketLength); start < recentEnd; start += options.BucketLength)
        {
            buckets.Add(UsualBucketEvaluator.Evaluate(start, byBucket.GetValueOrDefault(start, []), baseline, options));
        }

        return new UsualComparisonResult(
            true,
            ElevatedRunFinder.Find(buckets, b => b.Temperature, temperatureThreshold, options),
            compareIdleLoad ? ElevatedRunFinder.Find(buckets, b => b.IdleLoad, options.LoadDeltaPoints, options) : null);
    }
}

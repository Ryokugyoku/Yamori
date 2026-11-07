namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 生データから 1 分集計を、1 分集計から 1 時間集計を作る。終わった区間だけを集計し、どこまで済んだかをストアに記録する。
/// </summary>
/// <remarks>
/// 同じ区間を集計し直しても上書きになるだけなので、途中で止まっても次の回で続きから正しく集計できる。
/// </remarks>
public sealed class RollupJob
{
    /// <summary>区間が終わってから集計するまでの既定の待ち時間（まとめ書きの遅れを吸収する）。</summary>
    public static readonly TimeSpan DefaultSettleDelay = TimeSpan.FromSeconds(15);

    private readonly ITimeSeriesStore _store;
    private readonly RetentionPolicy _retention;
    private readonly long _settleDelayMs;

    /// <summary>
    /// ストアと保持期間を指定して作る。
    /// </summary>
    /// <param name="store">時系列のストア。</param>
    /// <param name="retention">保持期間（初回の集計を始める時刻を、元データの保持期間の範囲に限るために使う）。</param>
    /// <param name="settleDelay">区間が終わってから集計するまでの待ち時間。省略時は <see cref="DefaultSettleDelay"/>。</param>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> か <paramref name="retention"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="settleDelay"/> が負の値。</exception>
    public RollupJob(ITimeSeriesStore store, RetentionPolicy retention, TimeSpan? settleDelay = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(retention);
        var delay = settleDelay ?? DefaultSettleDelay;
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero, nameof(settleDelay));

        _store = store;
        _retention = retention;
        _settleDelayMs = (long)delay.TotalMilliseconds;
    }

    /// <summary>
    /// 終わった区間を集計する。
    /// </summary>
    /// <param name="nowMs">今の時刻（UTC の Unix ミリ秒）。</param>
    /// <param name="sensorIds">対象のセンサーの番号。</param>
    /// <returns>書き込んだ集計の数。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sensorIds"/> が null。</exception>
    public int Run(long nowMs, IReadOnlyCollection<uint> sensorIds)
    {
        ArgumentNullException.ThrowIfNull(sensorIds);

        var settled = Math.Max(0, nowMs - _settleDelayMs);
        var written = RollUp(
            TimeSeriesResolution.OneMinute,
            sourceRetention: _retention.Raw,
            available: settled,
            sensorIds,
            (id, from, to) => RollupCalculator.FromRaw(_store.ReadRaw(id, from, to), TimeSeriesResolutions.OneMinuteMs));

        // 1 時間集計は、1 分集計が済んだところまでしか作らない。
        var minuteWatermark = _store.GetRollupWatermark(TimeSeriesResolution.OneMinute) ?? 0;
        written += RollUp(
            TimeSeriesResolution.OneHour,
            sourceRetention: _retention.OneMinute,
            available: minuteWatermark,
            sensorIds,
            (id, from, to) => RollupCalculator.Combine(
                _store.ReadRollups(TimeSeriesResolution.OneMinute, id, from, to),
                TimeSeriesResolutions.OneHourMs));

        return written;
    }

    private int RollUp(
        TimeSeriesResolution target,
        TimeSpan sourceRetention,
        long available,
        IReadOnlyCollection<uint> sensorIds,
        Func<uint, long, long, IReadOnlyList<RollupPoint>> aggregate)
    {
        var bucketMs = TimeSeriesResolutions.BucketMs(target);
        var end = TimeSeriesResolutions.FloorToBucket(available, bucketMs);
        var start = _store.GetRollupWatermark(target)
            ?? TimeSeriesResolutions.FloorToBucket(RetentionJob.Cutoff(available, sourceRetention), bucketMs);
        if (end <= start)
        {
            return 0;
        }

        var written = 0;
        foreach (var sensorId in sensorIds)
        {
            var rollups = aggregate(sensorId, start, end);
            _store.WriteRollups(target, rollups);
            written += rollups.Count;
        }

        _store.SetRollupWatermark(target, end);
        return written;
    }
}

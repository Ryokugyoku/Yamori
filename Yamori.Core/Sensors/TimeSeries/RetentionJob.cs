namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 保持期間を過ぎたデータを、解像度ごとに消す。
/// </summary>
public sealed class RetentionJob
{
    private readonly ITimeSeriesStore _store;
    private readonly RetentionPolicy _policy;

    /// <summary>
    /// ストアと保持期間を指定して作る。
    /// </summary>
    /// <param name="store">時系列のストア。</param>
    /// <param name="policy">保持期間。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public RetentionJob(ITimeSeriesStore store, RetentionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(policy);
        _store = store;
        _policy = policy;
    }

    /// <summary>
    /// 保持期間を過ぎたデータを消す。
    /// </summary>
    /// <param name="nowMs">今の時刻（UTC の Unix ミリ秒）。</param>
    /// <param name="sensorIds">対象のセンサーの番号（マスタにあるすべてのセンサー）。</param>
    /// <returns>解像度ごとの、消した境目の時刻（この時刻より前を消した）。無期限の解像度は含めない。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sensorIds"/> が null。</exception>
    public IReadOnlyDictionary<TimeSeriesResolution, long> Run(long nowMs, IReadOnlyCollection<uint> sensorIds)
    {
        ArgumentNullException.ThrowIfNull(sensorIds);

        var cutoffs = new Dictionary<TimeSeriesResolution, long>();
        foreach (var resolution in Enum.GetValues<TimeSeriesResolution>())
        {
            if (_policy.For(resolution) is not { } retention)
            {
                continue;
            }

            var cutoff = Cutoff(nowMs, retention);
            _store.DeleteBefore(resolution, sensorIds, cutoff);
            cutoffs[resolution] = cutoff;
        }

        return cutoffs;
    }

    /// <summary>
    /// 保持期間から、消す境目の時刻を求める。
    /// </summary>
    /// <param name="nowMs">今の時刻（UTC の Unix ミリ秒）。</param>
    /// <param name="retention">保持期間。</param>
    /// <returns>境目の時刻。0 より前にはならない。</returns>
    public static long Cutoff(long nowMs, TimeSpan retention) => Math.Max(0, nowMs - (long)retention.TotalMilliseconds);
}

namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 読んだ値のうち記録するものを選んでためておき、まとめて時系列のストアに書き込む（数秒ごとに 1 回の WriteBatch）。
/// </summary>
/// <remarks>スレッドセーフ（値の受け取りと書き込みは別のスレッドから呼んでよい）。</remarks>
public sealed class TimeSeriesRecorder
{
    private readonly ITimeSeriesStore _store;
    private readonly LogDecisionFilter _filter;
    private readonly TimeSeriesResolution _resolution;
    private readonly Lock _lock = new();
    private List<TimeSeriesPoint> _pending = [];
    private IReadOnlyDictionary<SensorKey, SensorLogTarget> _targets = new Dictionary<SensorKey, SensorLogTarget>();
    private IReadOnlyDictionary<string, ushort> _sourceIds = new Dictionary<string, ushort>();

    /// <summary>
    /// 書き込み先と、記録するかどうかの決め方を指定して作る。
    /// </summary>
    /// <param name="store">書き込み先。</param>
    /// <param name="filter">記録するかどうかの決め方。</param>
    /// <param name="resolution">書き込む先の解像度。省略時は生データ。1 秒ごとの記録には <see cref="TimeSeriesResolution.OneSecond"/> を使う。</param>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> か <paramref name="filter"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="resolution"/> が集計の解像度。</exception>
    public TimeSeriesRecorder(ITimeSeriesStore store, LogDecisionFilter filter, TimeSeriesResolution resolution = TimeSeriesResolution.Raw)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(filter);
        if (!TimeSeriesResolutions.IsPointResolution(resolution))
        {
            throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "点を置く解像度ではありません。");
        }

        _store = store;
        _filter = filter;
        _resolution = resolution;
    }

    /// <summary>まだ書き込んでいない点の数を取得する。</summary>
    public int PendingCount
    {
        get
        {
            lock (_lock)
            {
                return _pending.Count;
            }
        }
    }

    /// <summary>
    /// 記録の対象と、取得元の番号を差し替える。カタログや記録の設定が変わったときに呼ぶ。
    /// </summary>
    /// <param name="targets">キーごとの記録の対象。ここにないセンサーは記録しない。</param>
    /// <param name="sourceIds">Provider の ID ごとの、取得元の番号。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public void UpdateTargets(IReadOnlyDictionary<SensorKey, SensorLogTarget> targets, IReadOnlyDictionary<string, ushort> sourceIds)
    {
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(sourceIds);
        lock (_lock)
        {
            _targets = targets;
            _sourceIds = sourceIds;
            _filter.RetainOnly(targets.Keys.ToHashSet());
        }
    }

    /// <summary>
    /// 読んだ値を受け取り、記録するものをためる。
    /// </summary>
    /// <param name="readings">読んだ値。</param>
    /// <exception cref="ArgumentNullException"><paramref name="readings"/> が null。</exception>
    public void Accept(IReadOnlyList<SensorReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);
        lock (_lock)
        {
            foreach (var reading in readings)
            {
                if (_targets.TryGetValue(reading.Key, out var target)
                    && _sourceIds.TryGetValue(reading.SourceProviderId, out var sourceId)
                    && _filter.ShouldRecord(reading, target.Policy))
                {
                    _pending.Add(new TimeSeriesPoint(target.SensorId, reading.TimestampUtc.ToUnixTimeMilliseconds(), reading.Value!.Value, sourceId));
                }
            }
        }
    }

    /// <summary>
    /// ためた点をまとめて書き込む。
    /// </summary>
    /// <returns>書き込んだ点の数。</returns>
    public int Flush()
    {
        List<TimeSeriesPoint> points;
        lock (_lock)
        {
            if (_pending.Count == 0)
            {
                return 0;
            }

            points = _pending;
            _pending = [];
        }

        try
        {
            _store.WritePoints(_resolution, points);
        }
        catch
        {
            // 書き込めなかった点は捨てずに戻し、次の回に書き込む。
            lock (_lock)
            {
                points.AddRange(_pending);
                _pending = points;
            }

            throw;
        }

        return points.Count;
    }
}

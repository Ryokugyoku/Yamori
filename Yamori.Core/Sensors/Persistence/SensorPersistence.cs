using System.Text.Json;
using Yamori.Core.Sensors.Master;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Sensors.Persistence;

/// <summary>
/// センサーの値の永続化をまとめて受け持つ。カタログをマスタへ反映し、読んだ値を時系列（生データと、一部のセンサーの 1 秒ごとの値）へまとめて書き込み、
/// 定期的に集計と保持期間の処理を行う。履歴の読み出しと記録の設定の変更の窓口にもなる。
/// </summary>
public sealed class SensorPersistence
{
    /// <summary>まとめて書き込む既定の間隔。</summary>
    public static readonly TimeSpan DefaultFlushInterval = TimeSpan.FromSeconds(5);

    /// <summary>集計する既定の間隔。</summary>
    public static readonly TimeSpan DefaultRollupInterval = TimeSpan.FromMinutes(1);

    /// <summary>保持期間の処理をする既定の間隔。</summary>
    public static readonly TimeSpan DefaultRetentionInterval = TimeSpan.FromHours(1);

    private const string Origin = "persistence";

    private readonly ITimeSeriesStore _store;
    private readonly TimeSeriesRecorder _recorder;
    private readonly TimeSeriesRecorder _fineRecorder;
    private readonly FineSeriesSelection _fineSelection;
    private readonly SensorCatalogSynchronizer _synchronizer;
    private readonly ISensorRepository _sensors;
    private readonly IEventLogRepository _events;
    private readonly RollupJob _rollup;
    private readonly RetentionJob _retention;
    private readonly IReadOnlyCollection<string> _providerIds;
    private readonly TimeProvider _timeProvider;
    private readonly Lock _catalogLock = new();
    private IReadOnlyList<SensorDescriptor> _catalog = [];

    /// <summary>
    /// 部品を指定して作る。
    /// </summary>
    /// <param name="store">時系列のストア。</param>
    /// <param name="recorder">生データのまとめ書き。</param>
    /// <param name="fineRecorder">1 秒ごとの値のまとめ書き（<see cref="TimeSeriesResolution.OneSecond"/> に書くもの）。</param>
    /// <param name="fineSelection">1 秒ごとに記録するセンサーの選び方。</param>
    /// <param name="synchronizer">カタログのマスタへの反映。</param>
    /// <param name="sensors">センサーのリポジトリ（記録の設定と、集計・削除の対象の番号）。</param>
    /// <param name="events">イベントの記録先。</param>
    /// <param name="rollup">集計の処理。</param>
    /// <param name="retention">保持期間の処理。</param>
    /// <param name="providerIds">登録しているすべての Provider の ID。</param>
    /// <param name="timeProvider">現在時刻とタイマーの提供元。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public SensorPersistence(
        ITimeSeriesStore store,
        TimeSeriesRecorder recorder,
        TimeSeriesRecorder fineRecorder,
        FineSeriesSelection fineSelection,
        SensorCatalogSynchronizer synchronizer,
        ISensorRepository sensors,
        IEventLogRepository events,
        RollupJob rollup,
        RetentionJob retention,
        IReadOnlyCollection<string> providerIds,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(recorder);
        ArgumentNullException.ThrowIfNull(fineRecorder);
        ArgumentNullException.ThrowIfNull(fineSelection);
        ArgumentNullException.ThrowIfNull(synchronizer);
        ArgumentNullException.ThrowIfNull(sensors);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(rollup);
        ArgumentNullException.ThrowIfNull(retention);
        ArgumentNullException.ThrowIfNull(providerIds);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _store = store;
        _recorder = recorder;
        _fineRecorder = fineRecorder;
        _fineSelection = fineSelection;
        _synchronizer = synchronizer;
        _sensors = sensors;
        _events = events;
        _rollup = rollup;
        _retention = retention;
        _providerIds = providerIds;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// カタログが変わったときに呼ぶ。マスタに反映し、記録の対象を差し替える。
    /// </summary>
    /// <param name="catalog">新しいカタログ。</param>
    /// <exception cref="ArgumentNullException"><paramref name="catalog"/> が null。</exception>
    public void OnCatalogChanged(IReadOnlyList<SensorDescriptor> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        lock (_catalogLock)
        {
            _catalog = catalog;
            RefreshTargets();
        }
    }

    /// <summary>
    /// 新しく読んだ値を受け取る。記録するものはためておき、<see cref="RunAsync"/> がまとめて書き込む。
    /// </summary>
    /// <param name="readings">読んだ値。</param>
    public void OnReadings(IReadOnlyList<SensorReading> readings)
    {
        _recorder.Accept(readings);
        _fineRecorder.Accept(readings);
    }

    /// <summary>
    /// キャンセルされるまで、まとめ書き・集計・保持期間の処理を定期的に行う。止めるときに、ためていた値を書き込む。
    /// </summary>
    /// <param name="cancellationToken">停止の合図。</param>
    /// <param name="flushInterval">まとめて書き込む間隔。省略時は <see cref="DefaultFlushInterval"/>。</param>
    /// <returns>停止したときに完了するタスク。キャンセルによる停止では例外を投げない。</returns>
    public async Task RunAsync(CancellationToken cancellationToken, TimeSpan? flushInterval = null)
    {
        var nextRollup = DateTimeOffset.MinValue;
        var nextRetention = DateTimeOffset.MinValue;
        using var timer = new PeriodicTimer(flushInterval ?? DefaultFlushInterval, _timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                var now = _timeProvider.GetUtcNow();
                Guard("flush", () => _recorder.Flush());
                Guard("flush-1s", () => _fineRecorder.Flush());
                if (now >= nextRetention)
                {
                    Guard("retention", () => _retention.Run(now.ToUnixTimeMilliseconds(), SensorIds()));
                    nextRetention = now + DefaultRetentionInterval;
                }

                if (now >= nextRollup)
                {
                    Guard("rollup", () => _rollup.Run(now.ToUnixTimeMilliseconds(), SensorIds()));
                    nextRollup = now + DefaultRollupInterval;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 停止の合図による正常終了。
        }
        finally
        {
            Guard("flush", () => _recorder.Flush());
            Guard("flush-1s", () => _fineRecorder.Flush());
        }
    }

    /// <summary>
    /// 履歴を古い順に読む。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <param name="fromUtc">範囲の始まり（含む）。</param>
    /// <param name="toUtc">範囲の終わり（含まない）。</param>
    /// <param name="resolution">解像度。</param>
    /// <returns>範囲内の点。記録していないセンサーなら空。</returns>
    public IReadOnlyList<SensorHistoryPoint> GetHistory(SensorKey key, DateTimeOffset fromUtc, DateTimeOffset toUtc, TimeSeriesResolution resolution)
    {
        if (_sensors.Find(key) is not { } sensor)
        {
            return [];
        }

        var from = Math.Max(0, fromUtc.ToUnixTimeMilliseconds());
        var to = Math.Max(from, toUtc.ToUnixTimeMilliseconds());
        return TimeSeriesResolutions.IsPointResolution(resolution)
            ? _store.ReadPoints(resolution, sensor.SensorId, from, to)
                .Select(p => new SensorHistoryPoint(DateTimeOffset.FromUnixTimeMilliseconds(p.TimestampMs), p.Value, p.Value, p.Value, 1))
                .ToArray()
            : _store.ReadRollups(resolution, sensor.SensorId, from, to)
                .Select(r => new SensorHistoryPoint(DateTimeOffset.FromUnixTimeMilliseconds(r.BucketStartMs), r.Min, r.Max, r.Average, r.Count))
                .ToArray();
    }

    /// <summary>
    /// 記録の設定を取得する。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <returns>記録の設定。マスタにないセンサーなら null。</returns>
    public SensorLogPolicy? GetLogPolicy(SensorKey key) => _sensors.Find(key)?.LogPolicy;

    /// <summary>
    /// 記録の設定を変え、すぐに記録の対象に反映する。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <param name="policy">新しい設定。</param>
    /// <returns>変えられたら true。マスタにないセンサーなら false。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="policy"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">間隔が 0 以下か、デッドバンドが負の値。</exception>
    public bool SetLogPolicy(SensorKey key, SensorLogPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(policy.Interval, TimeSpan.Zero, nameof(policy));
        ArgumentOutOfRangeException.ThrowIfNegative(policy.Deadband, nameof(policy));

        if (_sensors.UpdateLogPolicy(key, policy) is null)
        {
            return false;
        }

        lock (_catalogLock)
        {
            RefreshTargets();
        }

        _events.Append(new EventLogEntry(
            0,
            _timeProvider.GetUtcNow(),
            Origin,
            EventLevel.Information,
            "Log policy changed.",
            JsonSerializer.Serialize(new
            {
                sensor = key.ToString(),
                enabled = policy.Enabled,
                intervalMs = (long)policy.Interval.TotalMilliseconds,
                deadband = policy.Deadband,
            })));
        return true;
    }

    private void RefreshTargets()
    {
        var (targets, sourceIds) = _synchronizer.Synchronize(_catalog, _providerIds, _timeProvider.GetUtcNow());
        _recorder.UpdateTargets(targets, sourceIds);
        _fineRecorder.UpdateTargets(_fineSelection.Select(targets), sourceIds);
    }

    private uint[] SensorIds() => _sensors.GetAll().Select(s => s.SensorId).ToArray();

    private void Guard(string operation, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 1 回の失敗（ディスクの一時的なエラーなど）で記録を止めない。記録できる限り、イベントとして残す。
            try
            {
                _events.Append(new EventLogEntry(
                    0,
                    _timeProvider.GetUtcNow(),
                    Origin,
                    EventLevel.Error,
                    $"{operation} failed.",
                    JsonSerializer.Serialize(new { type = ex.GetType().FullName, message = ex.Message })));
            }
            catch (Exception)
            {
                // マスタにも書けない場合は、何もしない（Hot ストアの表示は続ける）。
            }
        }
    }
}

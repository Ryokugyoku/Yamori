using Yamori.Core.Storage;

namespace Yamori.Core.Monitoring;

/// <summary>
/// 一定の間隔でセンサーを読み、使用率の履歴を保存し続ける。
/// </summary>
public sealed class UsageMonitor
{
    /// <summary>
    /// 既定の測定間隔。
    /// </summary>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(10);

    private readonly IReadOnlyList<IUsageSensor> _sensors;
    private readonly IUsageHistoryStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _interval;

    /// <summary>
    /// 測定に使うセンサーと保存先を指定して作る。
    /// </summary>
    /// <param name="sensors">使用率を測るセンサー。</param>
    /// <param name="store">履歴の保存先。</param>
    /// <param name="timeProvider">現在時刻とタイマーの提供元。</param>
    /// <param name="interval">測定間隔。省略時は <see cref="DefaultInterval"/>。</param>
    /// <exception cref="ArgumentNullException"><paramref name="sensors"/>、<paramref name="store"/>、<paramref name="timeProvider"/> のいずれかが null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> が 0 以下。</exception>
    public UsageMonitor(
        IEnumerable<IUsageSensor> sensors,
        IUsageHistoryStore store,
        TimeProvider timeProvider,
        TimeSpan? interval = null)
    {
        ArgumentNullException.ThrowIfNull(sensors);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _interval = interval ?? DefaultInterval;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_interval, TimeSpan.Zero, nameof(interval));

        _sensors = sensors.ToArray();
        _store = store;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// 新しいサンプルを保存したときに発生する。UI スレッド以外から呼ばれることがある。
    /// </summary>
    public event EventHandler<IReadOnlyList<ComponentUsageSample>>? SamplesRecorded;

    /// <summary>
    /// すべてのセンサーを 1 回読み、測れた値を保存する。
    /// </summary>
    /// <returns>保存したサンプル。測れたものがなければ空。</returns>
    public IReadOnlyList<ComponentUsageSample> SampleOnce()
    {
        var now = _timeProvider.GetUtcNow();
        var samples = new List<ComponentUsageSample>(_sensors.Count);
        foreach (var sensor in _sensors)
        {
            if (sensor.ReadUsagePercent() is { } usage)
            {
                samples.Add(new ComponentUsageSample(sensor.Component, now, usage));
            }
        }

        if (samples.Count > 0)
        {
            _store.Append(samples);
            SamplesRecorded?.Invoke(this, samples);
        }

        return samples;
    }

    /// <summary>
    /// キャンセルされるまで、測定と保存を繰り返す。
    /// </summary>
    /// <param name="cancellationToken">停止の合図。</param>
    /// <returns>停止したときに完了するタスク。キャンセルによる停止では例外を投げない。</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_interval, _timeProvider);
        TrySampleOnce();

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                TrySampleOnce();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 停止の合図による正常終了。
        }
    }

    private void TrySampleOnce()
    {
        try
        {
            SampleOnce();
        }
        catch (IOException)
        {
            // 一時的な書き込み失敗（ウイルス対策ソフトによるロックなど）では監視を止めない。次の周期で再試行する。
        }
    }
}

using Yamori.Core.AppUsage.Sampling;
using Yamori.Core.AppUsage.Storage;

namespace Yamori.Core.AppUsage.Recording;

/// <summary>
/// 一定の間隔でアプリごとの CPU 使用率を測り、1 分ごとの平均を記録し続ける。古い記録は保持期間を過ぎたら消す。Helper の中で動かす。
/// </summary>
/// <remarks>
/// 測定の間隔が大きく空いた（スリープなど）ときは、その間の平均は意味がないため捨て、途中の 1 分を閉じる。
/// 書き込みに失敗しても、測定は止めない（その 1 分は記録されない）。
/// </remarks>
public sealed class AppCpuRecorder
{
    /// <summary>既定の測定間隔。</summary>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(10);

    /// <summary>既定の保持期間（センサーの 1 分集計と同じ 90 日）。</summary>
    public static readonly TimeSpan DefaultRetention = TimeSpan.FromDays(90);

    /// <summary>保持期間の処理をする間隔。</summary>
    public static readonly TimeSpan RetentionInterval = TimeSpan.FromHours(1);

    private readonly IAppCpuSampler _sampler;
    private readonly IAppUsageStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _retention;
    private readonly AppCpuMinuteAccumulator _accumulator = new();
    private DateTimeOffset? _lastTick;
    private DateTimeOffset _nextRetention = DateTimeOffset.MinValue;

    /// <summary>
    /// 必要な部品と間隔を指定して作る。
    /// </summary>
    /// <param name="sampler">測定の処理。</param>
    /// <param name="store">記録の保存先。</param>
    /// <param name="timeProvider">現在時刻とタイマーの提供元。</param>
    /// <param name="interval">測定間隔。省略時は <see cref="DefaultInterval"/>。</param>
    /// <param name="retention">保持期間。省略時は <see cref="DefaultRetention"/>。</param>
    /// <exception cref="ArgumentNullException">いずれかの部品が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">間隔か保持期間が 0 以下。</exception>
    public AppCpuRecorder(
        IAppCpuSampler sampler,
        IAppUsageStore store,
        TimeProvider timeProvider,
        TimeSpan? interval = null,
        TimeSpan? retention = null)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _interval = interval ?? DefaultInterval;
        _retention = retention ?? DefaultRetention;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_interval, TimeSpan.Zero, nameof(interval));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_retention, TimeSpan.Zero, nameof(retention));

        _sampler = sampler;
        _store = store;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// これより長く測定が空いたら、その間の値を捨てる長さを取得する（測定間隔の 3 倍）。
    /// </summary>
    public TimeSpan MaxGap => _interval * 3;

    /// <summary>
    /// キャンセルされるまで、測定と記録を繰り返す。止めるときに、途中の 1 分も記録する。
    /// </summary>
    /// <param name="cancellationToken">停止の合図。</param>
    /// <returns>停止したときに完了するタスク。キャンセルによる停止では例外を投げない。</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_interval, _timeProvider);
        try
        {
            Tick();
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                Tick();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 停止の合図による正常終了。
        }
        finally
        {
            Write(_accumulator.Flush());
        }
    }

    /// <summary>
    /// 1 回分の処理を行う。測って 1 分ごとにまとめ、終わった 1 分を記録する。保持期間の処理の時刻なら古い記録を消す。
    /// </summary>
    public void Tick()
    {
        var now = _timeProvider.GetUtcNow();
        var gap = _lastTick is { } last ? now - last : TimeSpan.Zero;
        _lastTick = now;

        AppCpuSample? sample;
        try
        {
            sample = _sampler.Sample(now);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // カウンターが一時的に読めないなど。次の回に測り直す。
            sample = null;
        }

        if (gap > MaxGap || gap < TimeSpan.Zero)
        {
            // スリープなどで測定が空いた（または時計が戻った）。この回の値は空いた間の平均なので捨てる。
            Write(_accumulator.Flush());
        }
        else if (sample is not null)
        {
            Write(_accumulator.Add(sample));
        }

        if (now >= _nextRetention)
        {
            _nextRetention = now + RetentionInterval;
            Guard(() => _store.DeleteBefore(now - _retention));
        }
    }

    private void Write(AppCpuMinute? minute)
    {
        if (minute is not null)
        {
            Guard(() => _store.WriteMinute(minute));
        }
    }

    private static void Guard(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // ディスクに書けないなど。記録は失われるが、測定は続ける。
        }
    }
}

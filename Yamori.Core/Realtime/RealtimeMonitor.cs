namespace Yamori.Core.Realtime;

/// <summary>
/// 一定の間隔で部品ごとの今の状態を測り、結果を知らせ続ける。記録はしない（リアルタイム表示用）。
/// </summary>
public sealed class RealtimeMonitor
{
    /// <summary>
    /// 既定の測定間隔。
    /// </summary>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(1);

    private readonly IRealtimeSnapshotSource _source;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _interval;

    /// <summary>
    /// 測定の方法と間隔を指定して作る。
    /// </summary>
    /// <param name="source">今の状態を測る処理。</param>
    /// <param name="timeProvider">タイマーの提供元。</param>
    /// <param name="interval">測定間隔。省略時は <see cref="DefaultInterval"/>。</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> か <paramref name="timeProvider"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> が 0 以下。</exception>
    public RealtimeMonitor(IRealtimeSnapshotSource source, TimeProvider timeProvider, TimeSpan? interval = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _interval = interval ?? DefaultInterval;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_interval, TimeSpan.Zero, nameof(interval));

        _source = source;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// 新しい測定結果が出たときに発生する。UI スレッド以外から呼ばれる。
    /// </summary>
    public event EventHandler<RealtimeSnapshot>? SnapshotTaken;

    /// <summary>
    /// キャンセルされるまで測定を繰り返す。測定の処理は、このメソッドを呼んだスレッドとタイマーのスレッドだけで使う。
    /// </summary>
    /// <param name="cancellationToken">停止の合図。</param>
    /// <returns>停止したときに完了するタスク。キャンセルによる停止では例外を投げない。</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_interval, _timeProvider);

        // 1 秒あたりの値は 2 回目の測定から分かるため、最初の 1 回は知らせずに基準にする。
        _source.Take();

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                SnapshotTaken?.Invoke(this, _source.Take());
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 停止の合図による正常終了。
        }
    }
}

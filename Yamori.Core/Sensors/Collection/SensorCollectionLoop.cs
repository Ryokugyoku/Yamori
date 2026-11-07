using Yamori.Core.Sensors.Hot;
using Yamori.Core.Sensors.Registry;

namespace Yamori.Core.Sensors.Collection;

/// <summary>
/// 一定の間隔で、カタログにあるすべてのセンサーを読み、Hot ストアに書き込み続ける。
/// ハードウェアの構成が変わったら、少し待ってからセンサーの宣言をやり直す。
/// </summary>
public sealed class SensorCollectionLoop : IDisposable
{
    /// <summary>既定の測定間隔。</summary>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(1);

    /// <summary>構成の変化の通知が止んでから、宣言をやり直すまでの既定の待ち時間。</summary>
    public static readonly TimeSpan DefaultRediscoverDelay = TimeSpan.FromSeconds(3);

    private readonly SensorRegistry _registry;
    private readonly IHotStore _hotStore;
    private readonly IHardwareChangeNotifier _changes;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _rediscoverDelay;
    private long _rediscoverAtTicks = long.MaxValue;

    /// <summary>
    /// 必要な部品と間隔を指定して作る。
    /// </summary>
    /// <param name="registry">センサーの統合。</param>
    /// <param name="hotStore">書き込み先。</param>
    /// <param name="changes">ハードウェアの構成の変化の通知。</param>
    /// <param name="timeProvider">現在時刻とタイマーの提供元。</param>
    /// <param name="interval">測定間隔。省略時は <see cref="DefaultInterval"/>。</param>
    /// <param name="rediscoverDelay">宣言をやり直すまでの待ち時間。省略時は <see cref="DefaultRediscoverDelay"/>。</param>
    /// <exception cref="ArgumentNullException">いずれかの部品が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">間隔が 0 以下か、待ち時間が負の値。</exception>
    public SensorCollectionLoop(
        SensorRegistry registry,
        IHotStore hotStore,
        IHardwareChangeNotifier changes,
        TimeProvider timeProvider,
        TimeSpan? interval = null,
        TimeSpan? rediscoverDelay = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(hotStore);
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _interval = interval ?? DefaultInterval;
        _rediscoverDelay = rediscoverDelay ?? DefaultRediscoverDelay;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_interval, TimeSpan.Zero, nameof(interval));
        ArgumentOutOfRangeException.ThrowIfLessThan(_rediscoverDelay, TimeSpan.Zero, nameof(rediscoverDelay));

        _registry = registry;
        _hotStore = hotStore;
        _changes = changes;
        _timeProvider = timeProvider;
        _changes.Changed += OnHardwareChanged;
    }

    /// <summary>
    /// センサーを宣言させてから、キャンセルされるまで測定と書き込みを繰り返す。
    /// </summary>
    /// <param name="cancellationToken">停止の合図。</param>
    /// <returns>停止したときに完了するタスク。キャンセルによる停止では例外を投げない。</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await DiscoverAsync(cancellationToken).ConfigureAwait(false);

            using var timer = new PeriodicTimer(_interval, _timeProvider);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await TickAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 停止の合図による正常終了。
        }
    }

    /// <summary>
    /// ハードウェアの構成の変化の通知の購読を解除する。
    /// </summary>
    public void Dispose() => _changes.Changed -= OnHardwareChanged;

    /// <summary>
    /// 1 回分の処理を行う。宣言のやり直しの時刻になっていればやり直し、すべてのセンサーを読んで書き込む。
    /// </summary>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>終わったときに完了するタスク。</returns>
    /// <exception cref="OperationCanceledException">中止された。</exception>
    public async Task TickAsync(CancellationToken cancellationToken)
    {
        var due = Interlocked.Read(ref _rediscoverAtTicks);
        if (due != long.MaxValue && _timeProvider.GetUtcNow().UtcTicks >= due)
        {
            // やり直しの途中で届いた通知は残し、次の回にもう一度やり直す。
            Interlocked.CompareExchange(ref _rediscoverAtTicks, long.MaxValue, due);
            await DiscoverAsync(cancellationToken).ConfigureAwait(false);
        }

        var readings = await _registry.ReadAllAsync(cancellationToken).ConfigureAwait(false);
        _hotStore.Append(readings);
    }

    private async Task DiscoverAsync(CancellationToken cancellationToken)
    {
        var catalog = await _registry.DiscoverAsync(cancellationToken).ConfigureAwait(false);
        _hotStore.RetainOnly(catalog.Select(d => d.Key).ToHashSet());
    }

    private void OnHardwareChanged(object? sender, EventArgs e) =>
        Interlocked.Exchange(ref _rediscoverAtTicks, (_timeProvider.GetUtcNow() + _rediscoverDelay).UtcTicks);
}

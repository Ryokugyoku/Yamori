using Yamori.Core.Realtime;

namespace Yamori.Core.Sensors.Providers.WindowsStandard;

/// <summary>
/// Windows の標準 API（PDH、D3DKMT、Win32 API、IOCTL、WMI）からセンサーの値を読む Provider。管理者権限は不要。
/// 既存の測定（<see cref="IRealtimeSnapshotSource"/>）の結果を、部品ごとの <see cref="ISnapshotSensorMapper"/> で共通のモデルに変える。
/// </summary>
/// <remarks>
/// <para><see cref="DiscoverAsync"/> では 2 回試しに測り（1 秒あたりの値は 2 回目から分かるため）、どちらかで値が出たセンサーを宣言する。
/// 宣言した後に値が出なかったセンサーは <see cref="SensorStatus.TemporarilyUnavailable"/> にする。</para>
/// <para>スレッドセーフ（測定は 1 つずつ行う）。</para>
/// </remarks>
public sealed class WindowsStandardProvider : ISensorProvider, IDisposable
{
    /// <summary>試しに測るときの、1 回目と 2 回目の間隔の既定値。</summary>
    public static readonly TimeSpan DefaultProbeInterval = TimeSpan.FromSeconds(1);

    private readonly Func<IRealtimeSnapshotSource> _createSource;
    private readonly IReadOnlyList<ISnapshotSensorMapper> _mappers;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _probeInterval;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IRealtimeSnapshotSource? _source;
    private bool _sourceFailed;
    private IReadOnlySet<SensorKey> _declared = new HashSet<SensorKey>();

    /// <summary>
    /// 測定の作り方と、取り出しの処理を指定して作る。
    /// </summary>
    /// <param name="createSource">測定の処理を作る処理。最初の <see cref="DiscoverAsync"/> で 1 回だけ呼び、このインスタンスが破棄する。</param>
    /// <param name="mappers">部品ごとの取り出しの処理。</param>
    /// <param name="timeProvider">現在時刻と待ち時間の提供元。</param>
    /// <param name="probeInterval">試しに測るときの間隔。省略時は <see cref="DefaultProbeInterval"/>。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="probeInterval"/> が負の値。</exception>
    public WindowsStandardProvider(
        Func<IRealtimeSnapshotSource> createSource,
        IEnumerable<ISnapshotSensorMapper> mappers,
        TimeProvider timeProvider,
        TimeSpan? probeInterval = null)
    {
        ArgumentNullException.ThrowIfNull(createSource);
        ArgumentNullException.ThrowIfNull(mappers);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _probeInterval = probeInterval ?? DefaultProbeInterval;
        ArgumentOutOfRangeException.ThrowIfLessThan(_probeInterval, TimeSpan.Zero, nameof(probeInterval));

        _createSource = createSource;
        _mappers = mappers.ToArray();
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public string ProviderId => SensorProviderIds.WindowsStandard;

    /// <inheritdoc />
    public async Task<IReadOnlyList<SensorDescriptor>> DiscoverAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var source = GetSource();
            if (source is null)
            {
                _declared = new HashSet<SensorKey>();
                return [];
            }

            var first = Map(source.Take());
            await Task.Delay(_probeInterval, _timeProvider, cancellationToken).ConfigureAwait(false);
            var second = Map(source.Take());

            var declared = second.Concat(first)
                .Where(s => s.Value is not null)
                .Select(s => s.Descriptor)
                .DistinctBy(d => d.Key)
                .ToArray();
            _declared = declared.Select(d => d.Key).ToHashSet();
            return declared;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SensorReading>> ReadAsync(IReadOnlyCollection<SensorKey> keys, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keys);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var declared = _declared;
            var source = _source;
            if (source is null)
            {
                return Fail(keys, declared, SensorStatus.TemporarilyUnavailable, _timeProvider.GetUtcNow());
            }

            RealtimeSnapshot snapshot;
            try
            {
                snapshot = source.Take();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Fail(keys, declared, SensorStatus.Error, _timeProvider.GetUtcNow());
            }

            var values = new Dictionary<SensorKey, double?>();
            foreach (var sample in Map(snapshot))
            {
                values.TryAdd(sample.Key, sample.Value);
            }

            return keys
                .Select(k => !declared.Contains(k) ? SensorReading.Failed(k, SensorStatus.Unsupported, snapshot.Timestamp, ProviderId)
                    : values.GetValueOrDefault(k) is { } value ? SensorReading.Ok(k, value, snapshot.Timestamp, ProviderId)
                    : SensorReading.Failed(k, SensorStatus.TemporarilyUnavailable, snapshot.Timestamp, ProviderId))
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// 測定の処理を破棄する。
    /// </summary>
    public void Dispose()
    {
        _source?.Dispose();
        _source = null;
        _gate.Dispose();
    }

    private IRealtimeSnapshotSource? GetSource()
    {
        if (_source is null && !_sourceFailed)
        {
            try
            {
                _source = _createSource();
            }
            catch (InvalidOperationException)
            {
                // パフォーマンスカウンターが壊れている PC など。この Provider は何も提供しない。
                _sourceFailed = true;
            }
        }

        return _source;
    }

    private List<SensorSample> Map(RealtimeSnapshot snapshot) =>
        _mappers.SelectMany(m => m.Map(snapshot)).ToList();

    private SensorReading[] Fail(IReadOnlyCollection<SensorKey> keys, IReadOnlySet<SensorKey> declared, SensorStatus status, DateTimeOffset now) =>
        keys.Select(k => SensorReading.Failed(k, declared.Contains(k) ? status : SensorStatus.Unsupported, now, ProviderId)).ToArray();
}

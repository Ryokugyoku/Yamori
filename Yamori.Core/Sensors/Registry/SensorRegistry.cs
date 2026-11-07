using Yamori.Core.Sensors.Providers;

namespace Yamori.Core.Sensors.Registry;

/// <summary>
/// すべての Provider のカタログをまとめて「利用可能なセンサーの一覧」を公開し、キーごとに優先する Provider から値を読む。
/// 優先する Provider が読めなかったキーは、次の Provider から読む（フォールバック）。
/// </summary>
/// <remarks>
/// <see cref="DiscoverAsync"/> と <see cref="ReadAsync"/> は別のスレッドから呼んでもよい。読み取りは、呼んだ時点のカタログで行う。
/// </remarks>
public sealed class SensorRegistry
{
    private readonly IReadOnlyList<ISensorProvider> _providers;
    private readonly IReadOnlyDictionary<string, ISensorProvider> _providersById;
    private readonly SensorPriorityPolicy _policy;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _discoverGate = new(1, 1);
    private volatile CatalogState _state = CatalogState.Empty;

    /// <summary>
    /// Provider と優先度の決め方を指定して作る。
    /// </summary>
    /// <param name="providers">登録する Provider。使えない Provider（PawnIO が入っていないなど）は含めないこと。</param>
    /// <param name="policy">優先度の決め方。</param>
    /// <param name="timeProvider">現在時刻の提供元（読めなかった結果の時刻に使う）。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    /// <exception cref="ArgumentException">同じ ID の Provider が 2 つ以上ある。</exception>
    public SensorRegistry(IEnumerable<ISensorProvider> providers, SensorPriorityPolicy policy, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _providers = providers.ToArray();
        var duplicate = _providers.GroupBy(p => p.ProviderId, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Provider の ID が重複しています: '{duplicate.Key}'", nameof(providers));
        }

        _providersById = _providers.ToDictionary(p => p.ProviderId, StringComparer.Ordinal);
        _policy = policy;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// カタログが変わったとき（<see cref="DiscoverAsync"/> の後）に発生する。引数は新しいカタログ。呼び出し元のスレッドで発生する。
    /// </summary>
    public event EventHandler<IReadOnlyList<SensorDescriptor>>? CatalogChanged;

    /// <summary>
    /// 利用可能なセンサーの一覧を、キーの文字列の順で取得する。同じキーを複数の Provider が提供する場合は、最も優先する Provider の説明を使う。
    /// </summary>
    public IReadOnlyList<SensorDescriptor> Catalog => _state.Catalog;

    /// <summary>
    /// 最後の <see cref="DiscoverAsync"/> で、カタログの宣言に失敗した Provider の ID を取得する（拡張センサーの状態表示などに使う）。
    /// </summary>
    public IReadOnlyCollection<string> FailedProviderIds => _state.FailedProviderIds;

    /// <summary>
    /// キーの説明を取得する。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <param name="descriptor">見つかった説明。</param>
    /// <returns>カタログにあれば true。</returns>
    public bool TryGetDescriptor(SensorKey key, out SensorDescriptor descriptor)
    {
        if (_state.Entries.TryGetValue(key, out var entry))
        {
            descriptor = entry.Descriptor;
            return true;
        }

        descriptor = null!;
        return false;
    }

    /// <summary>
    /// すべての Provider にセンサーを宣言させ、カタログを作り直す。宣言に失敗した Provider は、そのセンサーを提供しないものとして扱う。
    /// </summary>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>新しいカタログ。</returns>
    /// <exception cref="OperationCanceledException">中止された。</exception>
    public async Task<IReadOnlyList<SensorDescriptor>> DiscoverAsync(CancellationToken cancellationToken)
    {
        await _discoverGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var results = await Task.WhenAll(_providers.Select(p => DiscoverOneAsync(p, cancellationToken))).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var state = BuildState(results);
            _state = state;
            CatalogChanged?.Invoke(this, state.Catalog);
            return state.Catalog;
        }
        finally
        {
            _discoverGate.Release();
        }
    }

    /// <summary>
    /// カタログにあるすべてのセンサーの今の値を読む。
    /// </summary>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>キーごとの結果。</returns>
    /// <exception cref="OperationCanceledException">中止された。</exception>
    public Task<IReadOnlyList<SensorReading>> ReadAllAsync(CancellationToken cancellationToken)
    {
        var state = _state;
        return ReadCoreAsync(state, state.Entries.Keys.ToArray(), cancellationToken);
    }

    /// <summary>
    /// 指定したセンサーの今の値を読む。キーごとに優先する Provider から読み、<see cref="SensorStatus.Ok"/> にならなければ次の Provider から読む。
    /// </summary>
    /// <param name="keys">読むセンサーのキー。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>
    /// キーごとの結果。<see cref="SensorReading.SourceProviderId"/> は値を採用した Provider。
    /// どの Provider からも読めなかったキーは、最も優先する Provider の結果。カタログにないキーは <see cref="SensorStatus.Unsupported"/>。
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="keys"/> が null。</exception>
    /// <exception cref="OperationCanceledException">中止された。</exception>
    public Task<IReadOnlyList<SensorReading>> ReadAsync(IReadOnlyCollection<SensorKey> keys, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keys);
        return ReadCoreAsync(_state, keys, cancellationToken);
    }

    private async Task<IReadOnlyList<SensorReading>> ReadCoreAsync(
        CatalogState state,
        IReadOnlyCollection<SensorKey> keys,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var adopted = new Dictionary<SensorKey, SensorReading>();
        var firstFailure = new Dictionary<SensorKey, SensorReading>();
        var pending = new List<SensorKey>();

        foreach (var key in keys.Distinct())
        {
            if (state.Entries.ContainsKey(key))
            {
                pending.Add(key);
            }
            else
            {
                adopted[key] = SensorReading.Failed(key, SensorStatus.Unsupported, now, string.Empty);
            }
        }

        for (var rank = 0; pending.Count > 0; rank++)
        {
            var batches = pending
                .Where(k => rank < state.Entries[k].ProviderOrder.Count)
                .GroupBy(k => state.Entries[k].ProviderOrder[rank], StringComparer.Ordinal)
                .ToArray();
            if (batches.Length == 0)
            {
                break;
            }

            var results = await Task.WhenAll(batches.Select(b => ReadOneAsync(_providersById[b.Key], b.ToArray(), now, cancellationToken)))
                .ConfigureAwait(false);

            foreach (var reading in results.SelectMany(r => r))
            {
                if (reading.Status == SensorStatus.Ok)
                {
                    adopted[reading.Key] = reading;
                }
                else
                {
                    firstFailure.TryAdd(reading.Key, reading);
                }
            }

            pending = batches.SelectMany(b => b).Where(k => !adopted.ContainsKey(k)).ToList();
        }

        return keys.Distinct()
            .Select(k => adopted.TryGetValue(k, out var ok) ? ok
                : firstFailure.TryGetValue(k, out var failed) ? failed
                : SensorReading.Failed(k, SensorStatus.Error, now, string.Empty))
            .ToArray();
    }

    private static async Task<IReadOnlyList<SensorReading>> ReadOneAsync(
        ISensorProvider provider,
        IReadOnlyCollection<SensorKey> keys,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SensorReading> readings;
        try
        {
            readings = await provider.ReadAsync(keys, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 1 つの Provider の失敗（Helper の停止など）で全体を止めず、そのキーは次の Provider に回す。
            return keys.Select(k => SensorReading.Failed(k, SensorStatus.Error, now, provider.ProviderId)).ToArray();
        }

        var requested = keys.ToHashSet();
        var byKey = new Dictionary<SensorKey, SensorReading>();
        foreach (var reading in readings)
        {
            if (requested.Contains(reading.Key))
            {
                // 取得元は Provider の申告ではなく、実際に読んだ Provider で記録する。
                byKey.TryAdd(reading.Key, reading with { SourceProviderId = provider.ProviderId });
            }
        }

        // 返ってこなかったキーは「今は読めない」として扱う。
        return keys
            .Select(k => byKey.TryGetValue(k, out var r) ? r
                : SensorReading.Failed(k, SensorStatus.TemporarilyUnavailable, now, provider.ProviderId))
            .ToArray();
    }

    private static async Task<DiscoveryResult> DiscoverOneAsync(ISensorProvider provider, CancellationToken cancellationToken)
    {
        try
        {
            var descriptors = await provider.DiscoverAsync(cancellationToken).ConfigureAwait(false);
            return new DiscoveryResult(provider.ProviderId, descriptors, Failed: false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new DiscoveryResult(provider.ProviderId, [], Failed: true);
        }
    }

    private CatalogState BuildState(IReadOnlyList<DiscoveryResult> results)
    {
        var offered = new Dictionary<SensorKey, Dictionary<string, SensorDescriptor>>();
        foreach (var result in results)
        {
            foreach (var descriptor in result.Descriptors)
            {
                if (!offered.TryGetValue(descriptor.Key, out var byProvider))
                {
                    byProvider = new Dictionary<string, SensorDescriptor>(StringComparer.Ordinal);
                    offered[descriptor.Key] = byProvider;
                }

                // 説明の ProviderId は、宣言した Provider に揃える。
                byProvider.TryAdd(result.ProviderId, descriptor with { ProviderId = result.ProviderId });
            }
        }

        var entries = new Dictionary<SensorKey, CatalogEntry>();
        foreach (var (key, byProvider) in offered)
        {
            var order = _policy.Order(key, byProvider.Keys);
            entries[key] = new CatalogEntry(byProvider[order[0]], order);
        }

        var catalog = entries.Values
            .Select(e => e.Descriptor)
            .OrderBy(d => d.Key.ToString(), StringComparer.Ordinal)
            .ToArray();
        var failed = results.Where(r => r.Failed).Select(r => r.ProviderId).ToArray();
        return new CatalogState(entries, catalog, failed);
    }

    /// <summary>Provider 1 つの宣言の結果。</summary>
    /// <param name="ProviderId">Provider の ID。</param>
    /// <param name="Descriptors">宣言されたセンサー。失敗した場合は空。</param>
    /// <param name="Failed">宣言に失敗したかどうか。</param>
    private sealed record DiscoveryResult(string ProviderId, IReadOnlyList<SensorDescriptor> Descriptors, bool Failed);

    /// <summary>カタログのキー 1 つ分の情報。</summary>
    /// <param name="Descriptor">最も優先する Provider の説明。</param>
    /// <param name="ProviderOrder">このキーを提供する Provider の ID を、優先する順に並べたもの。</param>
    private sealed record CatalogEntry(SensorDescriptor Descriptor, IReadOnlyList<string> ProviderOrder);

    /// <summary>ある時点のカタログ全体。作り直すときは丸ごと差し替える。</summary>
    /// <param name="Entries">キーごとの情報。</param>
    /// <param name="Catalog">公開する一覧。</param>
    /// <param name="FailedProviderIds">宣言に失敗した Provider の ID。</param>
    private sealed record CatalogState(
        IReadOnlyDictionary<SensorKey, CatalogEntry> Entries,
        IReadOnlyList<SensorDescriptor> Catalog,
        IReadOnlyCollection<string> FailedProviderIds)
    {
        /// <summary>まだ宣言を行っていないときの、空のカタログを取得する。</summary>
        public static CatalogState Empty { get; } = new(new Dictionary<SensorKey, CatalogEntry>(), [], []);
    }
}

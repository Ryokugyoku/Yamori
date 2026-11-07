using Yamori.Core.Hardware;
using Yamori.Core.Realtime.Counters;

namespace Yamori.Core.Realtime.Collectors;

/// <summary>
/// ネットワークのパフォーマンスカウンターから、アダプターごとの今の送受信の状態を集める。
/// </summary>
public sealed class NetworkRealtimeCollector
{
    private const double BitsPerByte = 8.0;

    private readonly IPerformanceCounterSampler _sampler;
    private readonly Func<IReadOnlyList<NetworkAdapterInfo>> _adapters;

    /// <summary>
    /// カウンターの読み出し元と、アダプターの一覧の取得方法を指定して作る。
    /// </summary>
    /// <param name="sampler">カウンターの読み出し元。</param>
    /// <param name="adapters">アダプターの一覧を返す処理（読み取りに時間がかかるため、キャッシュしたものを渡す）。読めない場合は空を返すこと。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public NetworkRealtimeCollector(IPerformanceCounterSampler sampler, Func<IReadOnlyList<NetworkAdapterInfo>> adapters)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        ArgumentNullException.ThrowIfNull(adapters);
        _sampler = sampler;
        _adapters = adapters;
    }

    /// <summary>
    /// 表示するアダプターごとの今の状態を集める。
    /// </summary>
    /// <returns>アダプターごとの状態。名前の順。</returns>
    public IReadOnlyList<NetworkRealtime> Collect()
    {
        var received = _sampler.Read(CounterPaths.NetworkBytesReceivedPerSecond);
        var sent = _sampler.Read(CounterPaths.NetworkBytesSentPerSecond);
        var bandwidth = _sampler.Read(CounterPaths.NetworkCurrentBandwidth);

        var instances = received.Keys.Union(sent.Keys, StringComparer.OrdinalIgnoreCase).ToArray();
        if (instances.Length == 0)
        {
            return [];
        }

        var adapters = _adapters();
        var byInstance = adapters
            .GroupBy(a => NetworkCounterInstance.FromDescription(a.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        return instances
            .Select(instance =>
            {
                var adapter = byInstance.GetValueOrDefault(instance);
                var link = Lookup(bandwidth, instance);
                return (Instance: instance, Adapter: adapter, Link: link);
            })
            .Where(x => NetworkAdapterFilter.ShouldShow(x.Instance, x.Adapter, adapters.Count > 0, x.Link))
            .Select(x =>
            {
                var rx = Lookup(received, x.Instance);
                var tx = Lookup(sent, x.Instance);
                return new NetworkRealtime(x.Adapter?.Name ?? x.Instance, x.Adapter?.ConnectionKind ?? NetworkConnectionKind.Unknown)
                {
                    ReceivedBytesPerSecond = rx,
                    SentBytesPerSecond = tx,
                    LinkSpeedBitsPerSecond = x.Link is > 0 ? x.Link : null,
                    UsagePercent = ComputeUsage(rx, tx, x.Link),
                };
            })
            .OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// 送受信の合計をリンク速度で割った使用率を求める。
    /// </summary>
    /// <param name="receivedBytesPerSecond">受信の速さ（バイト／秒）。</param>
    /// <param name="sentBytesPerSecond">送信の速さ（バイト／秒）。</param>
    /// <param name="linkSpeedBitsPerSecond">リンク速度（ビット／秒）。</param>
    /// <returns>使用率（0〜100 の %）。送受信のどちらも分からないか、リンク速度が分からない場合は null。</returns>
    public static double? ComputeUsage(double? receivedBytesPerSecond, double? sentBytesPerSecond, double? linkSpeedBitsPerSecond)
    {
        if (linkSpeedBitsPerSecond is not > 0 || (receivedBytesPerSecond is null && sentBytesPerSecond is null))
        {
            return null;
        }

        var bits = ((receivedBytesPerSecond ?? 0) + (sentBytesPerSecond ?? 0)) * BitsPerByte;
        return Math.Clamp(100 * bits / linkSpeedBitsPerSecond.Value, 0, 100);
    }

    private static double? Lookup(IReadOnlyDictionary<string, double> values, string instance) =>
        values.TryGetValue(instance, out var value) ? value : null;
}

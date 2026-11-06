using Yamori.Core.Analysis;
using Yamori.Core.Hardware;
using Yamori.Core.Processes;

namespace Yamori.Core.Recommendations;

/// <summary>
/// 部品ごとの判定方針をまとめて実行し、次の PC のおすすめ一覧を作る。
/// </summary>
public sealed class NextPcAdvisor
{
    private readonly IReadOnlyList<IRecommendationPolicy> _policies;

    /// <summary>
    /// 使う判定方針を指定して作る。
    /// </summary>
    /// <param name="policies">部品ごとの判定方針。</param>
    /// <exception cref="ArgumentNullException"><paramref name="policies"/> が null。</exception>
    public NextPcAdvisor(IEnumerable<IRecommendationPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);
        _policies = policies.ToArray();
    }

    /// <summary>
    /// 判定方針が扱う部品の一覧を取得する。
    /// </summary>
    public IEnumerable<HardwareComponent> Components => _policies.Select(p => p.Component);

    /// <summary>
    /// 標準の判定方針（CPU とメモリ）で作る。
    /// </summary>
    /// <param name="options">判定の基準値。省略時は <see cref="RecommendationOptions.Default"/>。</param>
    /// <returns>標準構成の <see cref="NextPcAdvisor"/>。</returns>
    public static NextPcAdvisor CreateDefault(RecommendationOptions? options = null) =>
        new([new CpuRecommendationPolicy(options), new MemoryRecommendationPolicy(options)]);

    /// <summary>
    /// すべての部品についておすすめを判定する。
    /// </summary>
    /// <param name="inventory">今の PC のハードウェア一覧。</param>
    /// <param name="samplesByComponent">部品ごとの使用率の履歴。含まれない部品は履歴なしとして扱う。</param>
    /// <param name="topApps">資源を多く使っているアプリ。</param>
    /// <returns>判定方針の順に並んだおすすめ。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public IReadOnlyList<HardwareRecommendation> Advise(
        HardwareInventory inventory,
        IReadOnlyDictionary<HardwareComponent, IReadOnlyList<UsageSample>> samplesByComponent,
        IReadOnlyList<ProcessUsage> topApps)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(samplesByComponent);
        ArgumentNullException.ThrowIfNull(topApps);

        return _policies
            .Select(p => p.Evaluate(new RecommendationInput(
                inventory,
                samplesByComponent.TryGetValue(p.Component, out var samples) ? samples : [],
                topApps)))
            .ToArray();
    }
}

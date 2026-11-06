using Yamori.Core.Hardware;

namespace Yamori.Core.Recommendations;

/// <summary>
/// CPU の論理プロセッサ数（スレッド数）のおすすめを判定する。
/// </summary>
/// <remarks>
/// アプリごとの CPU 使用率は、まだ履歴として集計していないため、原因アプリは示さない。
/// </remarks>
public sealed class CpuRecommendationPolicy : TieredCapacityPolicy
{
    /// <summary>
    /// 判定の基準値を指定して作る。
    /// </summary>
    /// <param name="options">判定の基準値。省略時は <see cref="RecommendationOptions.Default"/>。</param>
    public CpuRecommendationPolicy(RecommendationOptions? options = null)
        : base(options)
    {
    }

    /// <inheritdoc />
    public override HardwareComponent Component => HardwareComponent.Cpu;

    /// <inheritdoc />
    protected override IReadOnlyList<int> Tiers => CapacityTiers.LogicalProcessors;

    /// <inheritdoc />
    protected override int? GetCurrentAmount(HardwareInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        return inventory.Cpu is { LogicalProcessors: > 0 } cpu ? cpu.LogicalProcessors : null;
    }
}

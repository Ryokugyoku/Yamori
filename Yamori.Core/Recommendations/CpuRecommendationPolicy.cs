using Yamori.Core.Hardware;

namespace Yamori.Core.Recommendations;

/// <summary>
/// CPU の論理プロセッサ数（スレッド数）のおすすめを判定する。
/// Helper の記録から、CPU の使用率が高かった時間に多く使っていたアプリを原因として示す。
/// </summary>
public sealed class CpuRecommendationPolicy : TieredCapacityPolicy
{
    private const int MaxCauses = 2;

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

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetMainCauses(RecommendationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.RecordedCauses.Take(MaxCauses).ToArray();
    }
}

using Yamori.Core.Hardware;

namespace Yamori.Core.Recommendations;

/// <summary>
/// メモリ容量のおすすめを判定する。メモリを多く使っているアプリを原因として示す。
/// </summary>
public sealed class MemoryRecommendationPolicy : TieredCapacityPolicy
{
    private const ulong BytesPerGigabyte = 1UL << 30;
    private const int MaxCauses = 2;

    /// <summary>
    /// 判定の基準値を指定して作る。
    /// </summary>
    /// <param name="options">判定の基準値。省略時は <see cref="RecommendationOptions.Default"/>。</param>
    public MemoryRecommendationPolicy(RecommendationOptions? options = null)
        : base(options)
    {
    }

    /// <inheritdoc />
    public override HardwareComponent Component => HardwareComponent.Memory;

    /// <inheritdoc />
    protected override IReadOnlyList<int> Tiers => CapacityTiers.MemoryGigabytes;

    /// <inheritdoc />
    protected override int? GetCurrentAmount(HardwareInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        return inventory.Memory is { TotalBytes: > 0 } memory
            ? (int)Math.Round((double)memory.TotalBytes / BytesPerGigabyte)
            : null;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetMainCauses(RecommendationInput input) =>
        input.TopApps.Take(MaxCauses).Select(a => a.Name).ToArray();
}

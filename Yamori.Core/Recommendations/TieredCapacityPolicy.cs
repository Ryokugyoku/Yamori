using Yamori.Core.Analysis;
using Yamori.Core.Hardware;

namespace Yamori.Core.Recommendations;

/// <summary>
/// 「高負荷の時間の割合」と「容量の段階」からおすすめを決める方針の共通部分。
/// 部品ごとの違い（今の量の求め方、段階、原因アプリ）は派生クラスが決める。
/// </summary>
public abstract class TieredCapacityPolicy : IRecommendationPolicy
{
    private readonly RecommendationOptions _options;

    /// <summary>
    /// 判定の基準値を指定して作る。
    /// </summary>
    /// <param name="options">判定の基準値。null の場合は <see cref="RecommendationOptions.Default"/>。</param>
    protected TieredCapacityPolicy(RecommendationOptions? options)
    {
        _options = options ?? RecommendationOptions.Default;
    }

    /// <inheritdoc />
    public abstract HardwareComponent Component { get; }

    /// <summary>
    /// おすすめの量を丸めるための段階を取得する。
    /// </summary>
    protected abstract IReadOnlyList<int> Tiers { get; }

    /// <inheritdoc />
    public HardwareRecommendation Evaluate(RecommendationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var highLoad = LoadAnalyzer.ComputeExposure(input.Samples, [_options.HighLoadThresholdPercent])[0];
        var level = LoadLevelClassifier.Classify(highLoad, _options);
        var current = GetCurrentAmount(input.Inventory);
        var recommended = RecommendAmount(level, current);
        var causes = level is RecommendationLevel.Borderline or RecommendationLevel.Insufficient
            ? GetMainCauses(input)
            : [];

        return new HardwareRecommendation(Component, level, highLoad, current, recommended, causes);
    }

    /// <summary>
    /// 今の PC の量を求める。
    /// </summary>
    /// <param name="inventory">ハードウェア一覧。</param>
    /// <returns>今の量。不明な場合は null。</returns>
    protected abstract int? GetCurrentAmount(HardwareInventory inventory);

    /// <summary>
    /// 高負荷の主な原因と思われるアプリを選ぶ。既定では原因を示さない。
    /// </summary>
    /// <param name="input">判定の材料。</param>
    /// <returns>アプリ名の一覧。</returns>
    protected virtual IReadOnlyList<string> GetMainCauses(RecommendationInput input) => [];

    private int? RecommendAmount(RecommendationLevel level, int? current) =>
        (level, current) switch
        {
            (RecommendationLevel.NotEnoughData, _) => null,
            (_, null) => null,
            (RecommendationLevel.Sufficient, { } c) => CapacityTiers.AtLeast(Tiers, c),
            (_, { } c) => CapacityTiers.Above(Tiers, c),
        };
}

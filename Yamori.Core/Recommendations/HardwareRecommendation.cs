using Yamori.Core.Analysis;
using Yamori.Core.Hardware;

namespace Yamori.Core.Recommendations;

/// <summary>
/// 次の PC を選ぶための、1 つの部品についてのおすすめ。
/// </summary>
/// <param name="Component">部品の種類。</param>
/// <param name="Level">今の性能が足りているかの判定。</param>
/// <param name="HighLoad">高負荷しきい値を超えていた時間の集計。</param>
/// <param name="CurrentAmount">今の量（メモリは GB、CPU は論理プロセッサ数）。不明な場合は null。</param>
/// <param name="RecommendedAmount">次の PC でおすすめする量（単位は <paramref name="CurrentAmount"/> と同じ）。判定できない場合は null。</param>
/// <param name="MainCauses">主な原因と思われるアプリの名前。分からない場合は空。</param>
public sealed record HardwareRecommendation(
    HardwareComponent Component,
    RecommendationLevel Level,
    ThresholdExposure HighLoad,
    int? CurrentAmount,
    int? RecommendedAmount,
    IReadOnlyList<string> MainCauses);

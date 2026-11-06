using Yamori.Core.Analysis;
using Yamori.Core.Hardware;
using Yamori.Core.Processes;

namespace Yamori.Core.Recommendations;

/// <summary>
/// 1 つの部品のおすすめを判定するための材料。
/// </summary>
/// <param name="Inventory">今の PC のハードウェア一覧。</param>
/// <param name="Samples">その部品の使用率の履歴。</param>
/// <param name="TopApps">資源を多く使っているアプリ。</param>
public sealed record RecommendationInput(
    HardwareInventory Inventory,
    IReadOnlyList<UsageSample> Samples,
    IReadOnlyList<ProcessUsage> TopApps);

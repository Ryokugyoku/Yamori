using Yamori.Core.Hardware;
using Yamori.Core.Processes;
using Yamori.Core.Recommendations;

namespace Yamori.Core.Reports;

/// <summary>
/// 画面に出す「今の PC」と「次の PC の基準」をまとめた結果。
/// </summary>
/// <param name="Inventory">今の PC のハードウェア一覧。</param>
/// <param name="Recommendations">部品ごとのおすすめ。</param>
/// <param name="TopApps">今、メモリを多く使っているアプリ。</param>
/// <param name="GeneratedAt">作成した時刻。</param>
public sealed record NextPcReport(
    HardwareInventory Inventory,
    IReadOnlyList<HardwareRecommendation> Recommendations,
    IReadOnlyList<ProcessUsage> TopApps,
    DateTimeOffset GeneratedAt);

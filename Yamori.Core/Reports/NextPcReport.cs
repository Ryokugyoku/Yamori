using Yamori.Core.Analysis.History;
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
/// <param name="HistoryOrigins">部品ごとの、判定に使った使用率の履歴の記録元。含まれない部品は <see cref="UsageHistoryOrigin.None"/>。</param>
public sealed record NextPcReport(
    HardwareInventory Inventory,
    IReadOnlyList<HardwareRecommendation> Recommendations,
    IReadOnlyList<ProcessUsage> TopApps,
    DateTimeOffset GeneratedAt,
    IReadOnlyDictionary<HardwareComponent, UsageHistoryOrigin> HistoryOrigins)
{
    /// <summary>
    /// 部品の判定に使った使用率の履歴の記録元を取得する。
    /// </summary>
    /// <param name="component">部品の種類。</param>
    /// <returns>記録元。履歴がなかった場合は <see cref="UsageHistoryOrigin.None"/>。</returns>
    public UsageHistoryOrigin OriginOf(HardwareComponent component) =>
        HistoryOrigins.TryGetValue(component, out var origin) ? origin : UsageHistoryOrigin.None;
}

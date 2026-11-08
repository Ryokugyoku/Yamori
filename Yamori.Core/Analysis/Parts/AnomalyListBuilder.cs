using Yamori.Core.Anomalies;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 分析画面の異常の一覧を、部品の種類で絞り込み、新しい順に並べる。部品の種類ごとの数も数える。
/// </summary>
/// <remarks>異常の種類（サーマルスロットリング、ふだんより悪い、今後足す種類）は区別せずに並べる。</remarks>
public static class AnomalyListBuilder
{
    /// <summary>
    /// 異常を絞り込んで並べる。
    /// </summary>
    /// <param name="anomalies">異常。</param>
    /// <param name="part">部品の種類。すべての部品なら null。</param>
    /// <param name="hardwareKey">部品のキー（同じ種類の部品が複数あるとき、1 つに絞る）。絞らないなら null。</param>
    /// <returns>新しく始まった順（同じ時刻なら番号の大きい順）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="anomalies"/> が null。</exception>
    public static IReadOnlyList<AnomalyRecord> Select(IEnumerable<AnomalyRecord> anomalies, AnalysisPart? part, string? hardwareKey = null)
    {
        ArgumentNullException.ThrowIfNull(anomalies);
        return anomalies
            .Where(a => part is null || AnalysisParts.From(a.HardwareKind) == part)
            .Where(a => hardwareKey is null || string.Equals(a.HardwareKey, hardwareKey, StringComparison.Ordinal))
            .OrderByDescending(a => a.StartedUtc)
            .ThenByDescending(a => a.AnomalyId)
            .ToArray();
    }

    /// <summary>
    /// 部品の種類ごとの異常の数を数える。「これは正常」とした異常は数えない。
    /// </summary>
    /// <param name="anomalies">異常。</param>
    /// <returns>部品の種類ごとの数。異常のない種類は含めない。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="anomalies"/> が null。</exception>
    public static IReadOnlyDictionary<AnalysisPart, int> CountByPart(IEnumerable<AnomalyRecord> anomalies)
    {
        ArgumentNullException.ThrowIfNull(anomalies);
        return anomalies
            .Where(a => !a.IsDismissed)
            .Select(a => AnalysisParts.From(a.HardwareKind))
            .OfType<AnalysisPart>()
            .GroupBy(p => p)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// グラフに色をつける異常の区間を選ぶ。範囲に重なる、その部品の異常（「これは正常」としたものを除く）。
    /// </summary>
    /// <param name="anomalies">異常。</param>
    /// <param name="hardwareKey">部品のキー。</param>
    /// <param name="range">グラフの範囲。</param>
    /// <returns>始まった順の異常。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="anomalies"/> が null。</exception>
    public static IReadOnlyList<AnomalyRecord> Bands(IEnumerable<AnomalyRecord> anomalies, string hardwareKey, AnalysisRange range)
    {
        ArgumentNullException.ThrowIfNull(anomalies);
        return anomalies
            .Where(a => !a.IsDismissed && string.Equals(a.HardwareKey, hardwareKey, StringComparison.Ordinal))
            .Where(a => range.Overlaps(a.StartedUtc, a.EndedUtc ?? a.LastSeenUtc))
            .OrderBy(a => a.StartedUtc)
            .ToArray();
    }
}

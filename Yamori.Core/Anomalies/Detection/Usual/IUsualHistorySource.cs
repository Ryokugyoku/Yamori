namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 「ふだんより悪い」の判定に使う、部品の 1 分ごとの記録を読む。
/// </summary>
public interface IUsualHistorySource
{
    /// <summary>
    /// 部品の 1 分ごとの記録を古い順に読む。
    /// </summary>
    /// <param name="part">部品。</param>
    /// <param name="fromUtc">範囲の始まり（含む）。</param>
    /// <param name="toUtc">範囲の終わり（含まない）。</param>
    /// <returns>負荷の記録がある分の記録。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="part"/> が null。</exception>
    IReadOnlyList<UsualMinute> Read(UsualPartDefinition part, DateTimeOffset fromUtc, DateTimeOffset toUtc);
}

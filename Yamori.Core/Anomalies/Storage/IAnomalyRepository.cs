namespace Yamori.Core.Anomalies.Storage;

/// <summary>
/// マスタの <c>anomaly</c>（異常の一覧）を読み書きする。実装はスレッドセーフであること。
/// </summary>
public interface IAnomalyRepository
{
    /// <summary>
    /// 新しい異常を書き込む。
    /// </summary>
    /// <param name="anomaly">異常。<see cref="AnomalyRecord.AnomalyId"/> は無視する。</param>
    /// <returns>書き込んだ行（番号付き）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="anomaly"/> が null。</exception>
    AnomalyRecord Insert(AnomalyRecord anomaly);

    /// <summary>
    /// 続いている異常の、最後に確かめた時刻・重さ・判断の理由を更新する。
    /// </summary>
    /// <param name="anomalyId">行の番号。</param>
    /// <param name="lastSeenUtc">最後に確かめた時刻。</param>
    /// <param name="severity">重さ。</param>
    /// <param name="evidence">判断の理由の数値。</param>
    /// <exception cref="ArgumentNullException"><paramref name="evidence"/> が null。</exception>
    void UpdateOngoing(long anomalyId, DateTimeOffset lastSeenUtc, AnomalySeverity severity, IReadOnlyDictionary<string, double> evidence);

    /// <summary>
    /// 異常を終わらせる。
    /// </summary>
    /// <param name="anomalyId">行の番号。</param>
    /// <param name="endedUtc">終わった時刻（最後に確かめた時刻にもする）。</param>
    /// <param name="severity">重さ。</param>
    /// <param name="evidence">判断の理由の数値。</param>
    /// <exception cref="ArgumentNullException"><paramref name="evidence"/> が null。</exception>
    void End(long anomalyId, DateTimeOffset endedUtc, AnomalySeverity severity, IReadOnlyDictionary<string, double> evidence);

    /// <summary>
    /// 続いたままになっている異常（Helper が止まる前に終わらせられなかったもの）を、最後に確かめた時刻で終わらせる。
    /// </summary>
    /// <returns>終わらせた数。</returns>
    int CloseOngoing();

    /// <summary>
    /// 同じ部品の同じ種類の異常で、最後に始まったものを取得する（終わった異常を続きとして開き直すかを決めるため）。
    /// </summary>
    /// <param name="kind">異常の種類。</param>
    /// <param name="hardwareKey">部品のキー。</param>
    /// <returns>最後に始まった異常。なければ null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hardwareKey"/> が null。</exception>
    AnomalyRecord? FindLatest(AnomalyKind kind, string hardwareKey);

    /// <summary>
    /// 終わった異常を、続いている状態に戻す（終わった時刻を消し、最後に確かめた時刻・重さ・判断の理由を更新する）。
    /// 「これは正常」の印と通知を出した時刻はそのまま残す。
    /// </summary>
    /// <param name="anomalyId">行の番号。</param>
    /// <param name="lastSeenUtc">最後に確かめた時刻。</param>
    /// <param name="severity">重さ。</param>
    /// <param name="evidence">判断の理由の数値。</param>
    /// <returns>戻したあとの異常。なければ null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="evidence"/> が null。</exception>
    AnomalyRecord? Reopen(long anomalyId, DateTimeOffset lastSeenUtc, AnomalySeverity severity, IReadOnlyDictionary<string, double> evidence);

    /// <summary>
    /// 異常を 1 つ取得する。
    /// </summary>
    /// <param name="anomalyId">行の番号。</param>
    /// <returns>異常。なければ null。</returns>
    AnomalyRecord? Find(long anomalyId);

    /// <summary>
    /// 期間に重なる異常を、新しく始まった順に取得する。
    /// </summary>
    /// <param name="fromUtc">期間の始まり（含む）。</param>
    /// <param name="toUtc">期間の終わり（含まない）。</param>
    /// <param name="includeDismissed">「これは正常」とした異常も含めるなら true。</param>
    /// <param name="limit">取得する最大の件数。</param>
    /// <returns>新しく始まった順の異常。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> が負の値。</exception>
    IReadOnlyList<AnomalyRecord> GetOverlapping(DateTimeOffset fromUtc, DateTimeOffset toUtc, bool includeDismissed, int limit);

    /// <summary>
    /// 「これは正常」の印を付ける、または外す。
    /// </summary>
    /// <param name="anomalyId">行の番号。</param>
    /// <param name="dismissedUtc">印を付けた時刻。外すなら null。</param>
    /// <returns>変えたあとの異常。なければ null。</returns>
    AnomalyRecord? SetDismissed(long anomalyId, DateTimeOffset? dismissedUtc);

    /// <summary>
    /// 通知を出した時刻を記録する。
    /// </summary>
    /// <param name="anomalyId">行の番号。</param>
    /// <param name="notifiedUtc">通知を出した時刻。</param>
    /// <returns>記録できたら true。なければ false。</returns>
    bool MarkNotified(long anomalyId, DateTimeOffset notifiedUtc);

    /// <summary>
    /// 同じ部品の同じ種類の異常で、最後に通知を出した時刻を取得する。
    /// </summary>
    /// <param name="kind">異常の種類。</param>
    /// <param name="hardwareKey">部品のキー。</param>
    /// <returns>最後に通知を出した時刻。一度も出していなければ null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hardwareKey"/> が null。</exception>
    DateTimeOffset? GetLastNotified(AnomalyKind kind, string hardwareKey);
}

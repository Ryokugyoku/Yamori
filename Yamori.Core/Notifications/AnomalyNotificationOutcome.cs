namespace Yamori.Core.Notifications;

/// <summary>
/// 異常の知らせを処理した結果（<see cref="AnomalyNotificationDispatcher.HandleAsync"/>）。
/// </summary>
public enum AnomalyNotificationOutcome
{
    /// <summary>通知の候補でない（終わった知らせ、Helper が通知しないとした知らせなど）。</summary>
    NotCandidate,

    /// <summary>設定がオフ、または同じ異常・同じ部品の同じ種類をすでに通知したため出さなかった。</summary>
    Suppressed,

    /// <summary>一時停止中・全画面中のため控えた、または出せなかった。異常は一覧に残る。</summary>
    Held,

    /// <summary>通知を出し、Helper に通知済みを記録した。</summary>
    Shown,
}

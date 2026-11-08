namespace Yamori.Core.Notifications;

/// <summary>
/// 通知を押したときに、アプリ本体へ渡す開き方の指定。
/// </summary>
/// <param name="Kind">何を開くか。</param>
/// <param name="AnomalyId">開く異常の番号。<see cref="NotificationActivationKind.OpenAnomaly"/> のときだけ使う。</param>
public sealed record NotificationActivationRequest(NotificationActivationKind Kind, long? AnomalyId = null)
{
    /// <summary>アプリを開くだけの指定。</summary>
    public static NotificationActivationRequest OpenApp { get; } = new(NotificationActivationKind.OpenApp);

    /// <summary>
    /// 指定した異常の画面を開く指定を作る。
    /// </summary>
    /// <param name="anomalyId">異常の番号。</param>
    /// <returns>開き方の指定。</returns>
    public static NotificationActivationRequest ForAnomaly(long anomalyId) =>
        new(NotificationActivationKind.OpenAnomaly, anomalyId);
}

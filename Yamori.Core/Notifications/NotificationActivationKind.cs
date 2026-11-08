namespace Yamori.Core.Notifications;

/// <summary>
/// 通知を押したときに、アプリ本体で何を開くか。
/// </summary>
public enum NotificationActivationKind
{
    /// <summary>アプリを開くだけ（いつもの最初の画面）。</summary>
    OpenApp,

    /// <summary>指定した異常の画面を開く。</summary>
    OpenAnomaly,
}

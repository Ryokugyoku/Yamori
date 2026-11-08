namespace Yamori.Core.Notifications;

/// <summary>
/// 異常の通知に出す文面。
/// </summary>
/// <param name="Title">見出し（部品と現象）。例：「CPU が熱で性能を落としています」。</param>
/// <param name="Body">本文（数値）。例：「96℃・クロック約 30% 低下・12 分間」。</param>
/// <param name="Activation">押したときにアプリ本体で開くもの（その異常）。</param>
public sealed record AnomalyToastContent(string Title, string Body, NotificationActivationRequest Activation);

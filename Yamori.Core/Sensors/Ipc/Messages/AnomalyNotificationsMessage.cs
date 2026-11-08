namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// 異常の通知の全体のオン・オフ（取得の返事と、変更の依頼）。
/// </summary>
/// <param name="Enabled">通知を出すなら true。</param>
public sealed record AnomalyNotificationsMessage(bool Enabled);

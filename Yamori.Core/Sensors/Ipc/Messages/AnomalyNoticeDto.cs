using Yamori.Core.Anomalies;

namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// パイプで送る異常の知らせ（<see cref="AnomalyNotice"/>。通知 <see cref="SensorNotificationKind.AnomalyChanged"/> の本文）。
/// </summary>
/// <param name="Change">何が変わったか。</param>
/// <param name="Anomaly">変わったあとの異常。</param>
/// <param name="ShouldNotify">Windows の通知を出すべきかどうか。</param>
public sealed record AnomalyNoticeDto(AnomalyChange Change, AnomalyDto? Anomaly, bool ShouldNotify);

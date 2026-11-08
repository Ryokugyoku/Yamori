using Yamori.Core.Anomalies;

namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// パイプで送る異常の設定（<see cref="AnomalySetting"/>）。
/// </summary>
/// <param name="Kind">異常の種類。</param>
/// <param name="NotificationsEnabled">通知を出すかどうか。</param>
/// <param name="ManualThreshold">手動で指定したしきい値。null なら自動。</param>
public sealed record AnomalySettingDto(AnomalyKind Kind, bool NotificationsEnabled, double? ManualThreshold);

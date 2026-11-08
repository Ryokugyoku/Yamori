namespace Yamori.Core.Anomalies;

/// <summary>
/// 異常の種類ごとの設定。管理者でない利用者も変えられる（安全に関わらない設定のため）。
/// </summary>
/// <param name="Kind">異常の種類。</param>
/// <param name="NotificationsEnabled">通知を出すかどうか。出さない場合も、異常は一覧に残す。</param>
/// <param name="ManualThreshold">
/// 手動で指定したしきい値。null なら自動（おすすめ）。意味は種類による（<see cref="AnomalySettingRules.ThresholdRange"/>）：
/// サーマルスロットリングは注意の温度（℃）、ふだんより悪いはふだんとの差（℃）。
/// </param>
public sealed record AnomalySetting(AnomalyKind Kind, bool NotificationsEnabled, double? ManualThreshold);

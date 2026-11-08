using Yamori.Core.Sensors;

namespace Yamori.Core.Anomalies.Detection;

/// <summary>
/// 規則が「今この部品で異常が起きている」と判断したもの。続いた時間の下限などは、規則の中で満たしてから返す。
/// </summary>
/// <param name="HardwareKey">部品のキー（<c>{hardwareKind}/{hardwareId}</c>。<see cref="Sensors.Master.HardwareRecord.StableKeyOf"/>）。</param>
/// <param name="HardwareKind">部品の種類。</param>
/// <param name="Severity">今の重さ。</param>
/// <param name="Evidence">判断の理由の数値（名前は <see cref="AnomalyEvidenceNames"/>）。</param>
/// <param name="SinceUtc">
/// 異常の状態が始まった時刻（続いた時間の下限を待ってから返す場合の、待ち始めの時刻）。null なら判定した時刻を始まりにする。
/// 新しい異常の始まりの時刻にだけ使う。
/// </param>
/// <param name="Notifiable">
/// 通知の対象にするかどうか。false なら一覧にだけ残す（電力・電流の制限だけでクロックも下がっていない場合など）。
/// 続いている間に true になったら、その時点で通知の対象になる。
/// </param>
public sealed record AnomalyFinding(
    string HardwareKey,
    HardwareKind HardwareKind,
    AnomalySeverity Severity,
    IReadOnlyDictionary<string, double> Evidence,
    DateTimeOffset? SinceUtc = null,
    bool Notifiable = true);

using Yamori.Core.Anomalies;

namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// パイプで送る異常 1 つ（<see cref="AnomalyRecord"/>）。
/// </summary>
/// <param name="AnomalyId">異常の番号。</param>
/// <param name="Kind">異常の種類。</param>
/// <param name="HardwareKey">部品のキー（<c>{hardwareKind}/{hardwareId}</c>）。</param>
/// <param name="HardwareKind">部品の種類。</param>
/// <param name="HardwareName">部品の表示名。</param>
/// <param name="StartedUtc">始まった時刻。</param>
/// <param name="LastSeenUtc">最後に異常を確かめた時刻。</param>
/// <param name="EndedUtc">終わった時刻。続いている間は null。</param>
/// <param name="Severity">重さ。</param>
/// <param name="Evidence">判断の理由の数値（名前と値の組）。</param>
/// <param name="DismissedUtc">「これは正常」とした時刻。していなければ null。</param>
/// <param name="NotifiedUtc">通知を出した時刻。出していなければ null。</param>
public sealed record AnomalyDto(
    long AnomalyId,
    AnomalyKind Kind,
    string HardwareKey,
    HardwareKind HardwareKind,
    string? HardwareName,
    DateTimeOffset StartedUtc,
    DateTimeOffset LastSeenUtc,
    DateTimeOffset? EndedUtc,
    AnomalySeverity Severity,
    Dictionary<string, double>? Evidence,
    DateTimeOffset? DismissedUtc,
    DateTimeOffset? NotifiedUtc);

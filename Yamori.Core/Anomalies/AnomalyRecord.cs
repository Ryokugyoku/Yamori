using Yamori.Core.Sensors;

namespace Yamori.Core.Anomalies;

/// <summary>
/// 異常の一覧（マスタの <c>anomaly</c>）の 1 行。同じ部品の同じ種類の異常が続いている間は、同じ 1 行を更新する。
/// </summary>
/// <param name="AnomalyId">行の番号。書き込む前は 0。</param>
/// <param name="Kind">異常の種類。</param>
/// <param name="HardwareKey">部品のキー（<c>{hardwareKind}/{hardwareId}</c>。例：<c>cpu/0</c>）。</param>
/// <param name="HardwareKind">部品の種類。</param>
/// <param name="HardwareName">部品の表示名（見つけたときの名前）。</param>
/// <param name="StartedUtc">始まった時刻。</param>
/// <param name="LastSeenUtc">最後に異常を確かめた時刻。</param>
/// <param name="EndedUtc">終わった時刻。続いている間は null。</param>
/// <param name="Severity">重さ（続いている間で最も重いもの）。</param>
/// <param name="Evidence">判断の理由の数値（名前と値の組。例：<c>temperatureCelsius</c> = 96）。名前は <see cref="AnomalyEvidenceNames"/>。</param>
/// <param name="DismissedUtc">「これは正常」とした時刻。していなければ null。</param>
/// <param name="NotifiedUtc">通知を出した時刻。出していなければ null。</param>
public sealed record AnomalyRecord(
    long AnomalyId,
    AnomalyKind Kind,
    string HardwareKey,
    HardwareKind HardwareKind,
    string HardwareName,
    DateTimeOffset StartedUtc,
    DateTimeOffset LastSeenUtc,
    DateTimeOffset? EndedUtc,
    AnomalySeverity Severity,
    IReadOnlyDictionary<string, double> Evidence,
    DateTimeOffset? DismissedUtc,
    DateTimeOffset? NotifiedUtc)
{
    /// <summary>まだ続いているかどうかを取得する。</summary>
    public bool IsOngoing => EndedUtc is null;

    /// <summary>「これは正常」としたかどうかを取得する。</summary>
    public bool IsDismissed => DismissedUtc is not null;

    /// <summary>続いた時間（続いている間は、最後に確かめた時刻まで）を取得する。</summary>
    public TimeSpan Duration => (EndedUtc ?? LastSeenUtc) - StartedUtc;
}

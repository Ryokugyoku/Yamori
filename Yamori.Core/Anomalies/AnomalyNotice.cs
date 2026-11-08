namespace Yamori.Core.Anomalies;

/// <summary>
/// 異常の知らせ。Helper がパイプで送り、通知専用の常駐プログラムと画面が受け取る。
/// </summary>
/// <param name="Change">何が変わったか。</param>
/// <param name="Anomaly">変わったあとの異常。</param>
/// <param name="ShouldNotify">
/// Windows の通知を出すべきかどうか（規則が通知の対象とした異常で、通知の全体と種類の設定がオンで、「これは正常」でなく、同じ部品の同じ種類の通知を 6 時間以内に出していない）。
/// 通知の対象でない異常（電力・電流の制限だけなど）は一覧にだけ残し、false になる。
/// 受け手は、全画面中や一時停止中でなければ通知を出し、出したら <see cref="Service.IAnomalyService.MarkNotifiedAsync"/> で知らせる。
/// </param>
public sealed record AnomalyNotice(AnomalyChange Change, AnomalyRecord Anomaly, bool ShouldNotify);

namespace Yamori.Core.Notifications;

/// <summary>
/// 異常の通知の文面のひな形（表示言語ごとの文字列）。<c>{0}</c> に部品の名前や数値を入れる。
/// 文面にアプリ名は入れない（ロック画面や画面共有に映るため）。
/// </summary>
/// <param name="ThermalThrottlingTitle">熱で性能を落としているときの見出し（<c>{0}</c> は部品の名前）。例：「{0} が熱で性能を落としています」。</param>
/// <param name="LimitThrottlingTitle">電力・電流の上限で性能を落としているときの見出し（<c>{0}</c> は部品の名前）。</param>
/// <param name="WorseThanUsualTitle">待機中の温度がふだんより高いときの見出し（<c>{0}</c> は部品の名前）。</param>
/// <param name="IdleLoadWorseThanUsualTitle">待機中の負荷だけがふだんより高いときの見出し（<c>{0}</c> は部品の名前）。例：「{0} の待機中の負荷がふだんより高くなっています」。</param>
/// <param name="CpuName">CPU の呼び名。</param>
/// <param name="GpuName">GPU の呼び名。</param>
/// <param name="OtherPartName">CPU・GPU 以外（温度ゾーンなど）の呼び名。例：「PC」。</param>
/// <param name="Temperature">温度（<c>{0}</c> は整数の ℃）。例：「{0}℃」。</param>
/// <param name="ClockDrop">クロックの下がり方（<c>{0}</c> は整数の %）。例：「クロック約 {0}% 低下」。</param>
/// <param name="DeltaFromUsual">ふだんとの温度の差（<c>{0}</c> は整数の ℃）。例：「ふだんより {0}℃ 高い」。</param>
/// <param name="IdleLoadDeltaFromUsual">待機中の負荷のふだんとの差（<c>{0}</c> は今の整数の %、<c>{1}</c> は差の整数のポイント）。例：「待機中の負荷 {0}%（ふだんより {1} ポイント高い）」。</param>
/// <param name="DurationSeconds">続いた時間が 1 分未満のとき（<c>{0}</c> は秒）。例：「{0} 秒間」。</param>
/// <param name="DurationMinutes">続いた時間が 1 分以上 1 時間未満のとき（<c>{0}</c> は分）。例：「{0} 分間」。</param>
/// <param name="DurationHours">続いた時間が 1 時間以上のとき（<c>{0}</c> は時間）。例：「{0} 時間」。</param>
/// <param name="Separator">本文の項目の区切り。例：「・」。</param>
public sealed record AnomalyToastTemplates(
    string ThermalThrottlingTitle,
    string LimitThrottlingTitle,
    string WorseThanUsualTitle,
    string IdleLoadWorseThanUsualTitle,
    string CpuName,
    string GpuName,
    string OtherPartName,
    string Temperature,
    string ClockDrop,
    string DeltaFromUsual,
    string IdleLoadDeltaFromUsual,
    string DurationSeconds,
    string DurationMinutes,
    string DurationHours,
    string Separator);

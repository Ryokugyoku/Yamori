namespace Yamori.Core.Anomalies.Explanation;

/// <summary>
/// 分析画面の「判断の理由」の一文のひな形（表示言語ごとの文字列）。理由の項目を区切りでつなぎ、文の型に入れる。
/// </summary>
/// <param name="Sentence">文の型（<c>{0}</c> は理由の項目をつないだもの、<c>{1}</c> は重さの名前）。例：「{0}だったため、「{1}」と判断しました。」。</param>
/// <param name="SentenceWithoutReasons">理由の数値がないときの文（<c>{0}</c> は重さの名前）。</param>
/// <param name="Separator">項目の区切り。例：「、」。</param>
/// <param name="LastSeparator">最後の項目の前の区切り。例：「、」（英語は「 and 」）。</param>
/// <param name="SeverityNotice">重さ「注意」の名前。</param>
/// <param name="SeverityWarning">重さ「警告」の名前。</param>
/// <param name="SeverityCritical">重さ「危険」の名前。</param>
/// <param name="ThermalLimit">熱による制限の印がオンだった時間（<c>{0}</c> は長さ）。</param>
/// <param name="AboveThreshold">注意の温度以上だった時間（<c>{0}</c> は長さ、<c>{1}</c> は注意の温度）。</param>
/// <param name="PowerLimit">電力の上限による制限の時間（<c>{0}</c> は長さ）。</param>
/// <param name="CurrentLimit">電流の上限による制限の時間（<c>{0}</c> は長さ）。</param>
/// <param name="PeakTemperature">最も高かった温度（<c>{0}</c> は温度）。</param>
/// <param name="ClockDrop">クロックの下がり方（<c>{0}</c> は整数の %）。</param>
/// <param name="DeltaFromUsual">ふだんとの差（<c>{0}</c> はふだんの値、<c>{1}</c> は差）。</param>
/// <param name="Temperature">温度の書き方（<c>{0}</c> は整数の ℃）。例：「{0}℃」。</param>
/// <param name="DurationSeconds">1 分未満の長さ（<c>{0}</c> は秒）。</param>
/// <param name="DurationMinutes">1 時間未満の長さ（<c>{0}</c> は分、<c>{1}</c> は秒）。秒が 0 でも同じ型を使う。</param>
/// <param name="DurationHours">1 時間以上の長さ（<c>{0}</c> は時間、<c>{1}</c> は分）。</param>
public sealed record AnomalyExplanationTemplates(
    string Sentence,
    string SentenceWithoutReasons,
    string Separator,
    string LastSeparator,
    string SeverityNotice,
    string SeverityWarning,
    string SeverityCritical,
    string ThermalLimit,
    string AboveThreshold,
    string PowerLimit,
    string CurrentLimit,
    string PeakTemperature,
    string ClockDrop,
    string DeltaFromUsual,
    string Temperature,
    string DurationSeconds,
    string DurationMinutes,
    string DurationHours);

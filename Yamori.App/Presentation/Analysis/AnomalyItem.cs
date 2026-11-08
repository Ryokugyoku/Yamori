using Yamori.Core.Anomalies;

namespace Yamori.Presentation.Analysis;

/// <summary>
/// 異常の一覧の 1 行の表示内容。
/// </summary>
/// <param name="Record">異常。</param>
/// <param name="Title">見出し（例：サーマルスロットリング）。</param>
/// <param name="PartText">部品の名前（例：CPU・Intel Core i7）。</param>
/// <param name="TimeText">いつからいつまでか（例：11/08 21:46〜21:49）。</param>
/// <param name="DurationText">続いた時間（例：3 分 13 秒）。続いている間は「続いています」を付ける。</param>
/// <param name="SeverityText">重さの名前。</param>
/// <param name="Level">重さの札の色。</param>
/// <param name="DismissedText">「これは正常」とした印の文字。していなければ空。</param>
/// <param name="AutomationName">読み上げの名前。</param>
public sealed record AnomalyItem(
    AnomalyRecord Record,
    string Title,
    string PartText,
    string TimeText,
    string DurationText,
    string SeverityText,
    StatusLevel Level,
    string DismissedText,
    string AutomationName)
{
    /// <summary>「これは正常」とした異常かどうかを取得する。</summary>
    public bool IsDismissed => Record.IsDismissed;
}

namespace Yamori.Presentation;

/// <summary>
/// 記録から求めた、CPU を多く使ったアプリ 1 件分の表示内容。
/// </summary>
/// <param name="Rank">一覧の中の順位（1 から）。</param>
/// <param name="Name">アプリのプロセス名。</param>
/// <param name="AverageText">期間の平均の CPU 使用率（例：平均 4.2%）。</param>
/// <param name="HighLoadText">高負荷の時間だけの平均（例：高負荷のとき 35.0%）。高負荷の時間がなければ空。</param>
/// <param name="RelativeShare">一覧の中でいちばん多いアプリを 100 としたときの平均（棒グラフの長さ）。</param>
public sealed record AppCpuHistoryItem(int Rank, string Name, string AverageText, string HighLoadText, double RelativeShare);

namespace Yamori.Presentation;

/// <summary>
/// 資源を多く使っているアプリ 1 件分の表示内容。
/// </summary>
/// <param name="Name">アプリのプロセス名。</param>
/// <param name="MemoryText">メモリ使用量（例：2.4 GB）。</param>
/// <param name="ProcessCountText">まとめたプロセスの数（例：18 プロセス）。</param>
/// <param name="RelativeShare">一覧の中でいちばん多いアプリを 100 としたときの使用量（棒グラフの長さ）。</param>
public sealed record AppUsageItem(string Name, string MemoryText, string ProcessCountText, double RelativeShare);

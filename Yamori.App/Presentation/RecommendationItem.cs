namespace Yamori.Presentation;

/// <summary>
/// 次の PC のおすすめ 1 件分の表示内容。
/// </summary>
/// <param name="Title">見出し。</param>
/// <param name="Explanation">数値の意味と根拠。</param>
public sealed record RecommendationItem(string Title, string Explanation);

namespace Yamori.Presentation;

/// <summary>
/// 次の PC のおすすめ 1 件分の表示内容。
/// </summary>
/// <param name="Glyph">部品の種類を表すアイコン（Segoe Fluent Icons の文字）。</param>
/// <param name="Title">見出し。</param>
/// <param name="Explanation">数値の意味と根拠。</param>
public sealed record RecommendationItem(string Glyph, string Title, string Explanation);

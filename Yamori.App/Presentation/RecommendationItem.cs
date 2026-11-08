namespace Yamori.Presentation;

/// <summary>
/// 次の PC のおすすめ 1 件分の表示内容。
/// </summary>
/// <param name="Glyph">部品の種類を表すアイコン（Segoe Fluent Icons の文字）。</param>
/// <param name="Title">見出し。</param>
/// <param name="Explanation">数値の意味と根拠。</param>
/// <param name="Level">判定の色分け。</param>
/// <param name="LevelText">判定を短く言い表した札の文字（例：余裕あり）。</param>
public sealed record RecommendationItem(string Glyph, string Title, string Explanation, StatusLevel Level, string LevelText)
{
    /// <summary>判定の札を出すかどうか。データが足りないときは見出しが同じことを言うため出さない。</summary>
    public bool HasLevel => Level != StatusLevel.Neutral;
}

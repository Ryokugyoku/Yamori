namespace Yamori.Presentation;

/// <summary>
/// 次の PC のおすすめ 1 件分の表示内容。
/// </summary>
/// <param name="Glyph">部品の種類を表すアイコン（Segoe Fluent Icons の文字）。</param>
/// <param name="Title">見出し。</param>
/// <param name="Explanation">数値の意味と根拠。</param>
/// <param name="Level">判定の色分け。</param>
/// <param name="LevelText">判定を短く言い表した札の文字（例：余裕あり）。</param>
/// <param name="SourceText">判定に使った記録の説明（例：常駐サービスの記録から）。履歴がなければ空。</param>
public sealed record RecommendationItem(
    string Glyph,
    string Title,
    string Explanation,
    StatusLevel Level,
    string LevelText,
    string SourceText)
{
    /// <summary>判定の札を出すかどうか。データが足りないときは見出しが同じことを言うため出さない。</summary>
    public bool HasLevel => Level != StatusLevel.Neutral;

    /// <summary>判定に使った記録の説明を出すかどうか。</summary>
    public bool HasSourceText => SourceText.Length > 0;
}

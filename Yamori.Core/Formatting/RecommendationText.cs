namespace Yamori.Core.Formatting;

/// <summary>
/// おすすめを画面に出すための文章。
/// </summary>
/// <param name="Title">見出し（例：「メモリ：次は 32GB 以上がおすすめ」）。</param>
/// <param name="Explanation">数値の意味と根拠の説明。</param>
public sealed record RecommendationText(string Title, string Explanation);

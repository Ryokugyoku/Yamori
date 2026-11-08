namespace Yamori.Presentation.Analysis;

/// <summary>
/// 集計値の表の 1 行（値の種類 1 つ）。
/// </summary>
/// <param name="Name">値の種類の名前（負荷・温度・クロック）。</param>
/// <param name="MinimumText">最小。</param>
/// <param name="AverageText">平均。</param>
/// <param name="MaximumText">最大。</param>
/// <param name="Percentile95Text">95%。</param>
public sealed record AnalysisStatItem(string Name, string MinimumText, string AverageText, string MaximumText, string Percentile95Text);

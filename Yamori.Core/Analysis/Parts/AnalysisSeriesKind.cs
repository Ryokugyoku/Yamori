namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 分析画面のグラフに重ねて描く値の種類。
/// </summary>
public enum AnalysisSeriesKind
{
    /// <summary>負荷（%）。</summary>
    Load,

    /// <summary>温度（℃）。</summary>
    Temperature,

    /// <summary>クロック（Hz）。</summary>
    Clock,
}

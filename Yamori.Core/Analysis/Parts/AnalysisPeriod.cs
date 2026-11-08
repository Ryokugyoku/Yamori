namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 分析画面で選べる期間。
/// </summary>
public enum AnalysisPeriod
{
    /// <summary>直近 24 時間。</summary>
    Last24Hours,

    /// <summary>直近 7 日。</summary>
    Last7Days,

    /// <summary>直近 90 日（1 分集計の保持期間）。</summary>
    Last90Days,

    /// <summary>日付で範囲を指定する。</summary>
    Custom,
}

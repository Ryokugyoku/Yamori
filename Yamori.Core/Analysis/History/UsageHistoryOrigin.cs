namespace Yamori.Core.Analysis.History;

/// <summary>
/// 負荷時間の分析に使った使用率の履歴が、どこで記録されたものか。
/// </summary>
public enum UsageHistoryOrigin
{
    /// <summary>履歴がなかった（Helper が記録していない、または Helper につながっていない）。</summary>
    None = 0,

    /// <summary>常駐サービス（Helper）の時系列。画面を閉じている間の記録も含む。</summary>
    HelperTimeSeries = 1,
}

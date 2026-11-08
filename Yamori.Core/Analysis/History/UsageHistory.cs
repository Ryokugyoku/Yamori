namespace Yamori.Core.Analysis.History;

/// <summary>
/// 1 つの部品の使用率の履歴と、その記録元。
/// </summary>
/// <param name="Origin">記録元。</param>
/// <param name="Samples">時刻順に並んだ使用率のサンプル。</param>
public sealed record UsageHistory(UsageHistoryOrigin Origin, IReadOnlyList<UsageSample> Samples)
{
    /// <summary>履歴がないことを表す値。</summary>
    public static UsageHistory Empty { get; } = new(UsageHistoryOrigin.None, []);

    /// <summary>サンプルが 1 つもないかどうかを取得する。</summary>
    public bool IsEmpty => Samples.Count == 0;
}

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 分析する時間の範囲（UTC）。
/// </summary>
/// <param name="FromUtc">始まり（含む）。</param>
/// <param name="ToUtc">終わり（含まない）。</param>
public readonly record struct AnalysisRange(DateTimeOffset FromUtc, DateTimeOffset ToUtc)
{
    /// <summary>長さを取得する。終わりが始まりより前なら 0。</summary>
    public TimeSpan Length => ToUtc > FromUtc ? ToUtc - FromUtc : TimeSpan.Zero;

    /// <summary>
    /// 区間がこの範囲に重なるかどうかを調べる。
    /// </summary>
    /// <param name="startUtc">区間の始まり。</param>
    /// <param name="endUtc">区間の終わり。</param>
    /// <returns>重なれば true。</returns>
    public bool Overlaps(DateTimeOffset startUtc, DateTimeOffset endUtc) => startUtc < ToUtc && endUtc >= FromUtc;
}

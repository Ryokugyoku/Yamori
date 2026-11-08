namespace Yamori.Core.AppUsage.Storage;

/// <summary>
/// 期間の中の、記録した 1 分の数と、アプリごとの使用率の合計。
/// </summary>
/// <param name="ObservedMinutes">記録した 1 分の数（Helper が測っていた時間）。</param>
/// <param name="HighLoadMinutes">そのうち、PC 全体の使用率がしきい値を超えていた 1 分の数。</param>
/// <param name="Apps">アプリごとの合計。順序は決めない。</param>
public sealed record AppCpuTotals(int ObservedMinutes, int HighLoadMinutes, IReadOnlyList<AppCpuTotal> Apps)
{
    /// <summary>記録がないことを表す値。</summary>
    public static AppCpuTotals Empty { get; } = new(0, 0, []);
}

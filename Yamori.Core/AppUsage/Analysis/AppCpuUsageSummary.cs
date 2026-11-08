namespace Yamori.Core.AppUsage.Analysis;

/// <summary>
/// 期間の中の、アプリごとの CPU 使用率の記録のまとめ。
/// </summary>
/// <param name="Observed">Helper が記録していた時間の合計。</param>
/// <param name="HighLoad">そのうち、PC 全体の CPU 使用率がしきい値を超えていた時間。</param>
/// <param name="HighLoadThresholdPercent">高負荷とみなしたしきい値（%）。</param>
/// <param name="Apps">CPU を多く使っていたアプリ。平均の使用率の大きい順。</param>
public sealed record AppCpuUsageSummary(
    TimeSpan Observed,
    TimeSpan HighLoad,
    double HighLoadThresholdPercent,
    IReadOnlyList<AppCpuUsage> Apps)
{
    /// <summary>記録がない（入れたばかり、または Helper につながっていない）ことを表す値。</summary>
    public static AppCpuUsageSummary Empty { get; } = new(TimeSpan.Zero, TimeSpan.Zero, 0, []);

    /// <summary>記録があるかどうかを取得する。</summary>
    public bool HasData => Observed > TimeSpan.Zero;

    /// <summary>
    /// 高負荷の時間に CPU を多く使っていたアプリを、多い順に選ぶ（次の PC のおすすめの「主な原因」に使う）。
    /// </summary>
    /// <param name="count">選ぶ最大の数。</param>
    /// <param name="minimumPercent">これ未満のアプリは選ばない（%）。</param>
    /// <returns>アプリ名。高負荷の時間がなければ空。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> が 0 以下。</exception>
    public IReadOnlyList<string> TopHighLoadApps(int count, double minimumPercent)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        return Apps
            .Where(a => a.HighLoadAveragePercent >= minimumPercent && a.HighLoadAveragePercent > 0)
            .OrderByDescending(a => a.HighLoadAveragePercent)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .Select(a => a.Name)
            .ToArray();
    }
}

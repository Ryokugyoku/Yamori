namespace Yamori.Core.Processes;

/// <summary>
/// プロセスをアプリ単位にまとめ、資源を多く使っている順に並べる。
/// </summary>
/// <remarks>
/// Chrome や Teams のように、1 つのアプリが多数のプロセスに分かれる場合でも、アプリとしての使用量が分かるようにする。
/// </remarks>
public static class ProcessUsageAggregator
{
    /// <summary>
    /// 集計から外すプロセス名。実体がアプリではないもの。
    /// </summary>
    private static readonly HashSet<string> ExcludedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Idle",
        "System",
        "Memory Compression",
        "Registry",
        "Secure System",
    };

    /// <summary>
    /// プロセス名ごとにメモリ使用量を合計し、多い順に返す。
    /// </summary>
    /// <param name="snapshots">プロセスごとの資源使用量。</param>
    /// <param name="top">返す件数の上限。</param>
    /// <returns>メモリ使用量の多い順に並んだアプリ単位の使用量。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshots"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="top"/> が 0 以下。</exception>
    public static IReadOnlyList<ProcessUsage> TopByMemory(IEnumerable<ProcessSnapshot> snapshots, int top)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        return snapshots
            .Where(s => !ExcludedNames.Contains(s.Name))
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProcessUsage(
                g.First().Name,
                g.Count(),
                g.Aggregate(0UL, (sum, s) => sum + s.WorkingSetBytes)))
            .OrderByDescending(u => u.WorkingSetBytes)
            .ThenBy(u => u.Name, StringComparer.OrdinalIgnoreCase)
            .Take(top)
            .ToArray();
    }
}

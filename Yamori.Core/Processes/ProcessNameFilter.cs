namespace Yamori.Core.Processes;

/// <summary>
/// プロセス名が、アプリとして集計する対象かどうかを決める。
/// </summary>
public static class ProcessNameFilter
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
        "_Total",
    };

    /// <summary>
    /// アプリとして集計する対象かどうかを調べる。
    /// </summary>
    /// <param name="name">プロセス名（拡張子なし）。</param>
    /// <returns>集計する対象なら true。空の名前や、実体がアプリではないもの（Idle、System など）は false。</returns>
    public static bool IsApplication(string? name) =>
        !string.IsNullOrWhiteSpace(name) && !ExcludedNames.Contains(name);
}

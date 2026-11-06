using System.Text;

namespace Yamori.Core.Storage;

/// <summary>
/// 保存先を変えたときに、これまでの履歴を新しい保存先へコピーする。元の履歴は消さない。
/// </summary>
public sealed class HistoryMigrator
{
    /// <summary>
    /// 履歴ファイルをコピーする。コピー先に同じ日のファイルがあれば、まだない行だけを追記してまとめる。
    /// </summary>
    /// <param name="sourceDirectory">コピー元のフォルダー。</param>
    /// <param name="destinationDirectory">コピー先のフォルダー。</param>
    /// <returns>コピーまたはまとめたファイルの数。</returns>
    /// <exception cref="ArgumentException">いずれかのフォルダーが空。</exception>
    /// <exception cref="IOException">読み書きに失敗した。</exception>
    public int Migrate(string sourceDirectory, string destinationDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        if (!Directory.Exists(sourceDirectory) || IsSameDirectory(sourceDirectory, destinationDirectory))
        {
            return 0;
        }

        Directory.CreateDirectory(destinationDirectory);
        var count = 0;
        foreach (var source in Directory.EnumerateFiles(sourceDirectory, CsvUsageHistoryStore.FileSearchPattern))
        {
            var destination = Path.Combine(destinationDirectory, Path.GetFileName(source));
            if (!File.Exists(destination))
            {
                File.Copy(source, destination);
                count++;
                continue;
            }

            var existing = new HashSet<string>(File.ReadLines(destination, Encoding.UTF8), StringComparer.Ordinal);
            var missing = File.ReadLines(source, Encoding.UTF8).Where(line => line.Length > 0 && existing.Add(line)).ToArray();
            if (missing.Length > 0)
            {
                File.AppendAllLines(destination, missing, Encoding.UTF8);
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 2 つのパスが同じフォルダーを指しているかを調べる。
    /// </summary>
    /// <param name="a">1 つ目のパス。</param>
    /// <param name="b">2 つ目のパス。</param>
    /// <returns>同じフォルダーなら true。</returns>
    public static bool IsSameDirectory(string a, string b) =>
        string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(b)),
            StringComparison.OrdinalIgnoreCase);
}

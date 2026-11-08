using System.Text.RegularExpressions;

namespace Yamori.Core.Storage.Legacy;

/// <summary>
/// 以前の版が残した使用率の記録（CSV）を消す。記録は Helper だけが行うようになったため、もう使わない。
/// </summary>
/// <remarks>
/// 消すのは、以前の版の名前の付け方（<c>usage-YYYY-MM-DD.csv</c>）に合うファイルだけ。ほかのファイルが残っているフォルダーは消さない。
/// 消せなかったファイル（使用中・権限がないなど）は残し、次に起動したときにもう一度試す。
/// </remarks>
public static partial class LegacyHistoryCleaner
{
    /// <summary>
    /// 指定した場所の古い CSV を消し、空になったフォルダーも消す。
    /// </summary>
    /// <param name="locations">場所の一覧（<see cref="LegacyHistoryLocator.Find"/>）。</param>
    /// <returns>消したファイルの数。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="locations"/> が null。</exception>
    public static int Clean(IEnumerable<LegacyHistoryLocation> locations)
    {
        ArgumentNullException.ThrowIfNull(locations);

        var deleted = 0;
        foreach (var location in locations)
        {
            if (!Directory.Exists(location.Directory))
            {
                continue;
            }

            deleted += DeleteCsvFiles(location.Directory);
            if (TryDeleteIfEmpty(location.Directory))
            {
                foreach (var parent in location.ParentsToRemoveIfEmpty)
                {
                    if (!TryDeleteIfEmpty(parent))
                    {
                        break;
                    }
                }
            }
        }

        return deleted;
    }

    private static int DeleteCsvFiles(string directory)
    {
        var deleted = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "usage-*.csv", SearchOption.TopDirectoryOnly))
            {
                if (!LegacyFileName().IsMatch(Path.GetFileName(file)))
                {
                    continue;
                }

                try
                {
                    File.Delete(file);
                    deleted++;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // 使用中などで消せないファイルは、次に起動したときにもう一度試す。
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // フォルダーを読めない場合は何もしない。
        }

        return deleted;
    }

    private static bool TryDeleteIfEmpty(string directory)
    {
        try
        {
            if (!Directory.Exists(directory) || Directory.EnumerateFileSystemEntries(directory).Any())
            {
                return false;
            }

            Directory.Delete(directory);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"^usage-\d{4}-\d{2}-\d{2}\.csv$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LegacyFileName();
}

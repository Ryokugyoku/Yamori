using System.Globalization;
using Yamori.Core.Storage;

namespace Yamori.Notifier;

/// <summary>
/// 通知専用の常駐プログラムで起きた想定外の失敗を、ファイルに書き残す（<c>%LOCALAPPDATA%\Yamori\notifier-errors.log</c>）。
/// 大きくなりすぎたら 1 世代だけ残して作り直す。書けなくても常駐は続ける。
/// </summary>
internal static class NotifierLog
{
    private const long MaxBytes = 256 * 1024;
    private static readonly object Gate = new();

    /// <summary>
    /// 例外を書き残す。
    /// </summary>
    /// <param name="origin">どこで起きたか。</param>
    /// <param name="exception">起きた例外。</param>
    public static void Write(string origin, Exception exception)
    {
        try
        {
            var path = Path.Combine(YamoriDataPaths.GetDataDirectory(), "notifier-errors.log");
            var line = string.Create(
                CultureInfo.InvariantCulture,
                $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} [{origin}] {exception}{Environment.NewLine}");

            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                if (File.Exists(path) && new FileInfo(path).Length > MaxBytes)
                {
                    File.Move(path, path + ".1", overwrite: true);
                }

                File.AppendAllText(path, line);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 書き残せなくても、通知の常駐は続ける。
        }
    }
}

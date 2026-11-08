using System;
using System.IO;
using System.Text;
using System.Threading;
using Yamori.Core.Storage;

namespace Yamori.Services;

/// <summary>
/// 画面で起きた想定外のエラーを、ユーザーのローカルフォルダーのファイルに書き残す。アプリを落とさずに続けたときでも、原因を後から調べられるようにする。
/// </summary>
/// <remarks>
/// ファイルは <see cref="YamoriDataPaths.GetErrorLogFilePath"/>。<see cref="MaxBytes"/> を超えたら 1 世代だけ（<c>.old</c>）残して書き直す。
/// 書き残せなくても例外は投げない（記録のせいで落ちないようにするため）。
/// </remarks>
public static class ErrorLog
{
    /// <summary>記録ファイルの大きさの上限（バイト）。</summary>
    public const long MaxBytes = 256 * 1024;

    private static readonly Lock Gate = new();

    /// <summary>
    /// エラーを 1 件書き残す。
    /// </summary>
    /// <param name="source">どこで起きたか（短い名前）。</param>
    /// <param name="exception">起きた例外。</param>
    public static void Write(string source, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(exception);

        var entry = new StringBuilder()
            .Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"))
            .Append(" [").Append(source).AppendLine("]")
            .AppendLine(exception.ToString())
            .AppendLine()
            .ToString();

        lock (Gate)
        {
            try
            {
                var path = YamoriDataPaths.GetErrorLogFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                RotateIfLarge(path);
                File.AppendAllText(path, entry, Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // 書き残せなくても、アプリの動作は続ける。
            }
        }
    }

    private static void RotateIfLarge(string path)
    {
        var file = new FileInfo(path);
        if (file.Exists && file.Length > MaxBytes)
        {
            File.Move(path, path + ".old", overwrite: true);
        }
    }
}

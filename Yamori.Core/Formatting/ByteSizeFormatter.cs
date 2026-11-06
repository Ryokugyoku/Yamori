using System.Globalization;

namespace Yamori.Core.Formatting;

/// <summary>
/// バイト数を、人が読みやすい単位つきの文字列にする。単位は 1024 進（GB = 2^30 バイト）。
/// </summary>
public static class ByteSizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB"];

    /// <summary>
    /// バイト数を単位つきの文字列にする。
    /// </summary>
    /// <param name="bytes">バイト数。</param>
    /// <returns>例：<c>15.8 GB</c>、<c>512 MB</c>。</returns>
    public static string Format(ulong bytes)
    {
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        var format = unit == 0 || value >= 100 ? "0" : "0.#";
        return value.ToString(format, CultureInfo.InvariantCulture) + " " + Units[unit];
    }
}

namespace Yamori.Core.Hardware;

/// <summary>
/// メーカーが値を設定し忘れたときに残る決まり文句（「To Be Filled By O.E.M.」など）を取り除く。
/// 意味のない文字列を、事実のように表示しないために使う。
/// </summary>
public static class OemPlaceholderFilter
{
    private static readonly HashSet<string> Placeholders = new(StringComparer.OrdinalIgnoreCase)
    {
        "To Be Filled By O.E.M.",
        "To be filled by O.E.M.",
        "Default string",
        "Default String",
        "System Product Name",
        "System manufacturer",
        "System Manufacturer",
        "System Version",
        "Base Board Product Name",
        "Base Board Manufacturer",
        "O.E.M.",
        "OEM",
        "Not Specified",
        "Not Applicable",
        "Not Available",
        "None",
        "Unknown",
        "Undefined",
        "N/A",
        "0",
        "00000000",
        "Manufacturer00",
        "Module00",
        "PartNum0",
        "SerNum0",
    };

    /// <summary>
    /// 決まり文句や空の文字列なら null を、そうでなければ前後の空白を除いた文字列を返す。
    /// </summary>
    /// <param name="value">確かめる文字列。</param>
    /// <returns>意味のある値なら前後の空白を除いた文字列、そうでなければ null。</returns>
    public static string? Clean(string? value)
    {
        var text = value?.Trim().TrimEnd('\0').Trim();
        if (string.IsNullOrEmpty(text) || Placeholders.Contains(text))
        {
            return null;
        }

        return text;
    }
}

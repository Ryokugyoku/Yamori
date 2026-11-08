using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Yamori.Controls;

/// <summary>
/// グラフの時間の軸の目盛りと、指したところの時刻の書き方を決める。表示言語の書き方に合わせる。
/// </summary>
public static partial class ChartTimeLabelFormatter
{
    /// <summary>
    /// 目盛りの文字列を作る。1 日より短い間隔なら時刻、1 日以上なら年のない日付。
    /// </summary>
    /// <param name="local">その地域の時刻。</param>
    /// <param name="step">目盛りの間隔。</param>
    /// <param name="culture">書き方の文化。</param>
    /// <returns>目盛りの文字列。</returns>
    public static string Tick(DateTimeOffset local, TimeSpan step, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return step < TimeSpan.FromDays(1)
            ? local.ToString(culture.DateTimeFormat.ShortTimePattern, culture)
            : local.ToString(MonthDayPattern(culture), culture);
    }

    /// <summary>
    /// 指したところの時刻の文字列を作る（年のない日付と時刻。1 秒ごとの記録なら秒まで）。
    /// </summary>
    /// <param name="local">その地域の時刻。</param>
    /// <param name="withSeconds">秒まで書くなら true。</param>
    /// <param name="culture">書き方の文化。</param>
    /// <returns>時刻の文字列。</returns>
    public static string Point(DateTimeOffset local, bool withSeconds, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        var time = withSeconds ? culture.DateTimeFormat.LongTimePattern : culture.DateTimeFormat.ShortTimePattern;
        return local.ToString(MonthDayPattern(culture) + " " + time, culture);
    }

    /// <summary>
    /// 短い日付の書き方から年を除く（例：<c>yyyy/MM/dd</c> → <c>MM/dd</c>、<c>dd/MM/yyyy</c> → <c>dd/MM</c>）。
    /// </summary>
    /// <param name="culture">書き方の文化。</param>
    /// <returns>年のない日付の書き方。</returns>
    public static string MonthDayPattern(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        var pattern = YearPart().Replace(culture.DateTimeFormat.ShortDatePattern, string.Empty).Trim();
        return pattern.Length > 0 ? pattern : "M/d";
    }

    [GeneratedRegex(@"[^dM]*y+[^dM]*")]
    private static partial Regex YearPart();
}

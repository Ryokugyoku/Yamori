using Yamori.Core.Localization;

namespace Yamori.Core.Formatting;

/// <summary>
/// 時間の長さを、日・時間・分のうち読みやすい単位の文字列にする。言語は現在の UI カルチャに従う。
/// </summary>
public static class DurationFormatter
{
    /// <summary>
    /// 1 日以上なら日、1 時間以上なら時間、それより短ければ分で表す（例：3.5日、2時間、15分）。
    /// </summary>
    /// <param name="duration">長さ。</param>
    /// <returns>表示用の文字列。</returns>
    public static string Format(TimeSpan duration) =>
        duration.TotalDays >= 1
            ? CoreText.Format("Duration_Days", duration.TotalDays)
            : duration.TotalHours >= 1
                ? CoreText.Format("Duration_Hours", duration.TotalHours)
                : CoreText.Format("Duration_Minutes", duration.TotalMinutes);
}

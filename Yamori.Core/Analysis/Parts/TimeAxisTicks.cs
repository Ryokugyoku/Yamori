namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// グラフの時間の軸の目盛りを決める。その地域の時刻で切りのよい時刻（5 分ごと、1 時間ごと、1 日ごとなど）に置く。
/// </summary>
public static class TimeAxisTicks
{
    private static readonly TimeSpan[] Steps =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(2),
        TimeSpan.FromHours(3),
        TimeSpan.FromHours(6),
        TimeSpan.FromHours(12),
        TimeSpan.FromDays(1),
        TimeSpan.FromDays(2),
        TimeSpan.FromDays(7),
        TimeSpan.FromDays(14),
        TimeSpan.FromDays(30),
    ];

    /// <summary>
    /// 目盛りの間隔を選ぶ。目盛りの数が <paramref name="maxTicks"/> 以下になる、いちばん短い間隔。
    /// </summary>
    /// <param name="range">範囲。</param>
    /// <param name="maxTicks">目盛りの最大の数。</param>
    /// <returns>間隔。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxTicks"/> が 0 以下。</exception>
    public static TimeSpan ChooseStep(AnalysisRange range, int maxTicks)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTicks);
        return Steps.FirstOrDefault(s => range.Length.Ticks / s.Ticks <= maxTicks, Steps[^1]);
    }

    /// <summary>
    /// 目盛りの時刻を求める。
    /// </summary>
    /// <param name="range">範囲。</param>
    /// <param name="timeZone">目盛りをそろえる地域の時刻。</param>
    /// <param name="maxTicks">目盛りの最大の数。</param>
    /// <returns>範囲の中の目盛りの時刻（古い順）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeZone"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxTicks"/> が 0 以下。</exception>
    public static IReadOnlyList<DateTimeOffset> Create(AnalysisRange range, TimeZoneInfo timeZone, int maxTicks)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        var step = ChooseStep(range, maxTicks);
        if (range.Length <= TimeSpan.Zero)
        {
            return [];
        }

        // その地域の時刻で、間隔の区切りにそろえる（1 日以上の間隔は 0 時に、それより短い間隔はその日の 0 時から数える）。
        var localStart = TimeZoneInfo.ConvertTime(range.FromUtc, timeZone);
        var midnight = new DateTime(localStart.Year, localStart.Month, localStart.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var stepTicks = step < TimeSpan.FromDays(1) ? step.Ticks : TimeSpan.FromDays(1).Ticks;
        var offset = localStart.DateTime - midnight;
        var first = midnight.AddTicks((offset.Ticks + stepTicks - 1) / stepTicks * stepTicks);

        var ticks = new List<DateTimeOffset>();
        var dayStep = step >= TimeSpan.FromDays(1) ? (int)step.TotalDays : 0;
        for (var local = first; ticks.Count <= maxTicks * 2; local = dayStep > 0 ? local.AddDays(dayStep) : local.Add(step))
        {
            if (timeZone.IsInvalidTime(local))
            {
                continue;
            }

            var utc = new DateTimeOffset(local, timeZone.GetUtcOffset(local)).ToUniversalTime();
            if (utc >= range.ToUtc)
            {
                break;
            }

            if (utc >= range.FromUtc)
            {
                ticks.Add(utc);
            }
        }

        return ticks;
    }
}

using Yamori.Core.Anomalies;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 分析画面の期間から、実際に読む時間の範囲を決める。
/// </summary>
public static class AnalysisRanges
{
    /// <summary>さかのぼれる最長の期間（1 分集計の保持期間）。</summary>
    public static readonly TimeSpan MaxLookback = TimeSpan.FromDays(90);

    /// <summary>異常を中心に表示するとき、異常の前後に足す最短の余白。</summary>
    public static readonly TimeSpan MinimumAnomalyPadding = TimeSpan.FromMinutes(5);

    /// <summary>異常を中心に表示するときの、最短の範囲の長さ。</summary>
    public static readonly TimeSpan MinimumAnomalyWindow = TimeSpan.FromMinutes(10);

    private static readonly TimeSpan Minute = TimeSpan.FromMinutes(1);

    /// <summary>
    /// 決まった期間（24 時間・7 日・90 日）の範囲を求める。
    /// </summary>
    /// <param name="period">期間。<see cref="AnalysisPeriod.Custom"/> は使えない。</param>
    /// <param name="nowUtc">今の時刻。</param>
    /// <returns>今までの範囲。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="period"/> が決まった期間でない。</exception>
    public static AnalysisRange ForPeriod(AnalysisPeriod period, DateTimeOffset nowUtc)
    {
        var length = period switch
        {
            AnalysisPeriod.Last24Hours => TimeSpan.FromDays(1),
            AnalysisPeriod.Last7Days => TimeSpan.FromDays(7),
            AnalysisPeriod.Last90Days => MaxLookback,
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "日付で指定する期間は ForDates を使ってください。"),
        };
        return new AnalysisRange(nowUtc - length, nowUtc);
    }

    /// <summary>
    /// 日付で指定した範囲を求める。始まりの日の 0 時から、終わりの日の翌日の 0 時まで（その地域の時刻で）。
    /// 今より先と、<see cref="MaxLookback"/> より前は切り詰める。日付が逆なら入れ替える。
    /// </summary>
    /// <param name="fromDate">始まりの日。</param>
    /// <param name="toDate">終わりの日（その日を含む）。</param>
    /// <param name="timeZone">日付を読む地域の時刻。</param>
    /// <param name="nowUtc">今の時刻。</param>
    /// <returns>範囲。切り詰めると空になるときは、直近 24 時間。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="timeZone"/> が null。</exception>
    public static AnalysisRange ForDates(DateOnly fromDate, DateOnly toDate, TimeZoneInfo timeZone, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        if (toDate < fromDate)
        {
            (fromDate, toDate) = (toDate, fromDate);
        }

        var from = StartOfDay(fromDate, timeZone);
        var to = StartOfDay(toDate.AddDays(1), timeZone);
        var earliest = nowUtc - MaxLookback;
        from = from < earliest ? earliest : from;
        to = to > nowUtc ? nowUtc : to;
        return from < to ? new AnalysisRange(from, to) : ForPeriod(AnalysisPeriod.Last24Hours, nowUtc);
    }

    /// <summary>
    /// 異常を中心に表示する範囲を求める。異常の前後に、続いた時間と同じだけ（最短 <see cref="MinimumAnomalyPadding"/>）の余白を足す。
    /// 今より先は切り詰め、最短でも <see cref="MinimumAnomalyWindow"/> にする。
    /// </summary>
    /// <param name="anomaly">異常。</param>
    /// <param name="nowUtc">今の時刻。</param>
    /// <returns>範囲。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="anomaly"/> が null。</exception>
    public static AnalysisRange AroundAnomaly(AnomalyRecord anomaly, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(anomaly);

        var start = anomaly.StartedUtc;
        var end = anomaly.EndedUtc ?? anomaly.LastSeenUtc;
        var duration = end > start ? end - start : TimeSpan.Zero;
        var padding = duration > MinimumAnomalyPadding ? duration : MinimumAnomalyPadding;

        var to = end + padding;
        to = to > nowUtc ? nowUtc : to;
        var from = start - padding;
        if (to - from < MinimumAnomalyWindow)
        {
            from = to - MinimumAnomalyWindow;
        }

        return new AnalysisRange(from, to);
    }

    /// <summary>
    /// アプリごとの CPU の記録（1 分ごと）を読む範囲を求める。範囲を 1 分の区切りまで広げる（1 分より短い異常でも、その 1 分の記録を読むため）。
    /// </summary>
    /// <param name="range">分析する範囲。</param>
    /// <returns>1 分の区切りにそろえた範囲。</returns>
    public static AnalysisRange ForAppUsage(AnalysisRange range)
    {
        var from = Floor(range.FromUtc);
        var to = Floor(range.ToUtc);
        if (to < range.ToUtc || to <= from)
        {
            to += Minute;
        }

        return new AnalysisRange(from, to);
    }

    private static DateTimeOffset StartOfDay(DateOnly date, TimeZoneInfo timeZone)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        if (timeZone.IsInvalidTime(local))
        {
            // 夏時間の始まりで 0 時がない日は、1 時間後から数える。
            local = local.AddHours(1);
        }

        return new DateTimeOffset(local, timeZone.GetUtcOffset(local)).ToUniversalTime();
    }

    private static DateTimeOffset Floor(DateTimeOffset value) =>
        new(value.UtcTicks - (value.UtcTicks % Minute.Ticks), TimeSpan.Zero);
}

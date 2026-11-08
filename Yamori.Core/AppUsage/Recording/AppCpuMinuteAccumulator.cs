namespace Yamori.Core.AppUsage.Recording;

/// <summary>
/// 測った値を 1 分ごとにまとめ、1 分間の平均（<see cref="AppCpuMinute"/>）にする。スレッドセーフではない。
/// </summary>
/// <remarks>
/// 測定は、測った時刻が含まれる 1 分に入れる。その 1 分に一度も出てこなかったアプリは、その測定では 0% として平均する。
/// 記録の量を抑えるため、1 分ごとに使用率の大きい上位 <see cref="MaxAppsPerMinute"/> 件の、平均が
/// <see cref="MinimumAppPercent"/> 以上のアプリだけを残す。
/// </remarks>
public sealed class AppCpuMinuteAccumulator
{
    /// <summary>1 分ごとに残すアプリの最大数。</summary>
    public const int MaxAppsPerMinute = 10;

    /// <summary>1 分ごとに残すアプリの、平均の使用率の下限（%）。</summary>
    public const double MinimumAppPercent = 0.1;

    private static readonly long TicksPerMinute = TimeSpan.TicksPerMinute;

    private readonly Dictionary<string, (string Name, double Sum)> _apps = new(StringComparer.OrdinalIgnoreCase);
    private long _minuteTicks = long.MinValue;
    private double _totalSum;
    private int _count;

    /// <summary>
    /// 測った値を加える。前の 1 分が終わっていれば、その平均を返す。
    /// </summary>
    /// <param name="sample">測った値。</param>
    /// <returns>終わった 1 分の平均。終わっていなければ null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sample"/> が null。</exception>
    public AppCpuMinute? Add(AppCpuSample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        var minuteTicks = sample.TimestampUtc.UtcTicks / TicksPerMinute * TicksPerMinute;
        AppCpuMinute? finished = null;
        if (_count > 0 && minuteTicks != _minuteTicks)
        {
            // 時計が戻った場合も、それまでの分を閉じて新しく始める。
            finished = Flush();
        }

        _minuteTicks = minuteTicks;
        _totalSum += sample.TotalPercent;
        _count++;
        foreach (var app in sample.Apps)
        {
            if (!double.IsFinite(app.CpuPercent) || app.CpuPercent <= 0)
            {
                continue;
            }

            _apps[app.Name] = _apps.TryGetValue(app.Name, out var current)
                ? (current.Name, current.Sum + app.CpuPercent)
                : (app.Name, app.CpuPercent);
        }

        return finished;
    }

    /// <summary>
    /// 途中の 1 分を閉じて、その平均を返す（記録を止めるときや、測定が途切れたときに使う）。
    /// </summary>
    /// <returns>途中の 1 分の平均。測った値がなければ null。</returns>
    public AppCpuMinute? Flush()
    {
        if (_count == 0)
        {
            return null;
        }

        var count = _count;
        var apps = _apps.Values
            .Select(a => new AppCpuShare(a.Name, a.Sum / count))
            .Where(a => a.CpuPercent >= MinimumAppPercent)
            .OrderByDescending(a => a.CpuPercent)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Take(MaxAppsPerMinute)
            .ToArray();
        var minute = new AppCpuMinute(new DateTimeOffset(_minuteTicks, TimeSpan.Zero), _totalSum / count, count, apps);

        _apps.Clear();
        _totalSum = 0;
        _count = 0;
        return minute;
    }
}

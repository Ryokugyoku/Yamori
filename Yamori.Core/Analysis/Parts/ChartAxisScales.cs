using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// グラフの値の軸の目盛りを決める。負荷は 0〜100%、温度は記録と注意の温度が入る切りのよい範囲、クロックは 0 から記録の最大まで。
/// </summary>
public static class ChartAxisScales
{
    /// <summary>目盛りの数の目安。</summary>
    public const int TargetTickCount = 4;

    /// <summary>
    /// 値の種類と記録から目盛りを決める。
    /// </summary>
    /// <param name="kind">値の種類。</param>
    /// <param name="points">記録。</param>
    /// <param name="thresholdCelsius">温度の軸に入れる注意の温度（℃）。なければ null。</param>
    /// <returns>目盛り。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="points"/> が null。</exception>
    public static ChartAxisScale For(AnalysisSeriesKind kind, IReadOnlyList<SensorHistoryPoint> points, double? thresholdCelsius)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (kind == AnalysisSeriesKind.Load)
        {
            return new ChartAxisScale(0, 100, 25);
        }

        var values = points.Where(p => double.IsFinite(p.Min) && double.IsFinite(p.Max)).ToArray();
        var low = values.Length > 0 ? values.Min(p => p.Min) : 0;
        var high = values.Length > 0 ? values.Max(p => p.Max) : 1;
        if (kind == AnalysisSeriesKind.Temperature)
        {
            if (thresholdCelsius is { } threshold && double.IsFinite(threshold))
            {
                low = Math.Min(low, threshold);
                high = Math.Max(high, threshold);
            }

            return Nice(Math.Max(0, low - 5), high + 5, TargetTickCount);
        }

        return Nice(0, Math.Max(high, 1), TargetTickCount);
    }

    /// <summary>
    /// 範囲を含む、切りのよい目盛りを決める（間隔は 1・2・2.5・5 の 10 のべき乗倍）。
    /// </summary>
    /// <param name="minimum">含める最小の値。</param>
    /// <param name="maximum">含める最大の値。</param>
    /// <param name="targetTicks">間隔の数の目安。</param>
    /// <returns>目盛り。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="targetTicks"/> が 0 以下。</exception>
    public static ChartAxisScale Nice(double minimum, double maximum, int targetTicks)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetTicks);
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum))
        {
            return new ChartAxisScale(0, 1, 0.25);
        }

        if (maximum < minimum)
        {
            (minimum, maximum) = (maximum, minimum);
        }

        if (maximum - minimum < 1e-9)
        {
            maximum = minimum + 1;
        }

        var rough = (maximum - minimum) / targetTicks;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        var step = new[] { 1, 2, 2.5, 5, 10 }.Select(f => f * magnitude).First(s => s >= rough);
        var low = Math.Floor(minimum / step) * step;
        var high = Math.Ceiling(maximum / step) * step;
        return new ChartAxisScale(low, high, step);
    }
}

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// グラフの値の軸の目盛り。
/// </summary>
/// <param name="Minimum">下限。</param>
/// <param name="Maximum">上限。</param>
/// <param name="Step">目盛りの間隔。</param>
public sealed record ChartAxisScale(double Minimum, double Maximum, double Step)
{
    /// <summary>
    /// 目盛りの値を、下限から上限まで取得する。
    /// </summary>
    /// <returns>目盛りの値。</returns>
    public IReadOnlyList<double> Ticks()
    {
        var ticks = new List<double>();
        for (var value = Minimum; value <= Maximum + (Step * 1e-6); value += Step)
        {
            ticks.Add(Math.Round(value, 9));
        }

        return ticks;
    }
}

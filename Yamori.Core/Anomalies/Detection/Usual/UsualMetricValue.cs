namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 区切り 1 つの値と、比べたふだんの値。
/// </summary>
/// <param name="Value">区切りの値（℃ か %）。</param>
/// <param name="Baseline">ふだんの値（同じ単位）。</param>
public readonly record struct UsualMetricValue(double Value, double Baseline)
{
    /// <summary>ふだんとの差（高ければ正）を取得する。</summary>
    public double Delta => Value - Baseline;
}

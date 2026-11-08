namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// ふだんより高い区切りが続いた期間。
/// </summary>
/// <param name="StartUtc">最初の「高い」区切りの始まり。</param>
/// <param name="EndUtc">最後の「高い」区切りの終わり。</param>
/// <param name="ElevatedDuration">「高い」区切りの長さの合計（判定できない区切りは含まない）。</param>
/// <param name="Value">「高い」区切りの値の平均（℃ か %）。</param>
/// <param name="Baseline">「高い」区切りのふだんの値の平均（同じ単位）。</param>
public sealed record ElevatedRun(DateTimeOffset StartUtc, DateTimeOffset EndUtc, TimeSpan ElevatedDuration, double Value, double Baseline)
{
    /// <summary>ふだんとの差の平均を取得する。</summary>
    public double Delta => Value - Baseline;
}

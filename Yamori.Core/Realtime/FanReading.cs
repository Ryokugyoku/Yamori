namespace Yamori.Core.Realtime;

/// <summary>
/// 回転数を読めたファン 1 つの今の値。
/// </summary>
/// <param name="Name">ファンの名前（どの部品のファンか）。</param>
/// <param name="Rpm">今の回転数（RPM）。0 は止まっている（低負荷のときに止める機種がある）。</param>
/// <param name="MaxRpm">最大回転数（RPM）。分からない場合は null。</param>
public sealed record FanReading(string Name, int Rpm, int? MaxRpm)
{
    /// <summary>最大回転数に対する割合（%）を取得する。最大回転数が分からない場合は null。</summary>
    public double? SpeedPercent => MaxRpm is > 0 and var max ? Math.Clamp(100.0 * Rpm / max, 0, 100) : null;
}

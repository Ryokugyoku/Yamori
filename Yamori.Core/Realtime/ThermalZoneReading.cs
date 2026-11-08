namespace Yamori.Core.Realtime;

/// <summary>
/// ACPI の温度センサー（サーマル ゾーン）1 つの今の値。
/// </summary>
/// <param name="Name">Windows が付けたセンサーの名前（例：<c>\_TZ.TZ00</c>）。</param>
/// <param name="TemperatureCelsius">温度（℃）。マザーボードが報告する値で、CPU の温度そのものとは限らない。</param>
/// <param name="PassiveLimitPercent">熱による CPU の性能の制限（%）。100 なら制限なし。分からない場合は null。</param>
/// <param name="ThrottleReasons">制限の理由（Windows の「Throttle Reasons」の値。0 なら制限なし）。分からない場合は null。</param>
public sealed record ThermalZoneReading(string Name, double TemperatureCelsius, double? PassiveLimitPercent, uint? ThrottleReasons)
{
    /// <summary>熱による制限がかかっているかどうかを取得する。分からない場合は null。</summary>
    public bool? IsThrottling => ThrottleReasons is { } reasons ? reasons != 0 : null;
}

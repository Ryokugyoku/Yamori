namespace Yamori.Core.Realtime;

/// <summary>
/// ACPI の温度センサー（サーマル ゾーン）1 つの今の値。
/// </summary>
/// <param name="Name">Windows が付けたセンサーの名前（例：<c>\_TZ.TZ00</c>）。</param>
/// <param name="TemperatureCelsius">温度（℃）。マザーボードが報告する値で、CPU の温度そのものとは限らない。</param>
/// <param name="PassiveLimitPercent">熱による CPU の性能の制限（%）。100 なら制限なし。分からない場合は null。</param>
/// <param name="IsThrottling">熱による制限がかかっているかどうか。分からない場合は null。</param>
public sealed record ThermalZoneReading(string Name, double TemperatureCelsius, double? PassiveLimitPercent, bool? IsThrottling);

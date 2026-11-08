namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 「ふだんより悪い」の判定に使う、1 分ごとの記録（1 分集計の平均）。
/// </summary>
/// <param name="TimestampUtc">その 1 分の始まり。</param>
/// <param name="LoadPercent">負荷（%）。</param>
/// <param name="TemperatureCelsius">温度（℃）。記録がなければ null。</param>
public readonly record struct UsualMinute(DateTimeOffset TimestampUtc, double LoadPercent, double? TemperatureCelsius);

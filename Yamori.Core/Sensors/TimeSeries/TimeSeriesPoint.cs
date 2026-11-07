namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 生データの 1 点。
/// </summary>
/// <param name="SensorId">センサーの番号（マスタで採番したもの）。</param>
/// <param name="TimestampMs">時刻（UTC の Unix ミリ秒）。</param>
/// <param name="Value">値（基準単位）。</param>
/// <param name="SourceId">値を読んだ取得元の番号（マスタの <c>source</c>）。</param>
public readonly record struct TimeSeriesPoint(uint SensorId, long TimestampMs, double Value, ushort SourceId);

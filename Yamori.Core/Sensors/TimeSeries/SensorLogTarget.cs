namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 記録の対象のセンサー 1 つの、時系列での番号と記録の設定。
/// </summary>
/// <param name="SensorId">時系列で使う番号（マスタで採番したもの）。</param>
/// <param name="Policy">記録の設定。</param>
public sealed record SensorLogTarget(uint SensorId, SensorLogPolicy Policy);

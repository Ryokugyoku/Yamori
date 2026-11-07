namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 集計の 1 区間。
/// </summary>
/// <param name="SensorId">センサーの番号。</param>
/// <param name="BucketStartMs">区間の始まり（UTC の Unix ミリ秒）。</param>
/// <param name="Min">最小値。</param>
/// <param name="Max">最大値。</param>
/// <param name="Average">平均値。</param>
/// <param name="Count">集計した生データの点の数。</param>
public readonly record struct RollupPoint(uint SensorId, long BucketStartMs, double Min, double Max, double Average, uint Count);

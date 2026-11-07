namespace Yamori.Core.Sensors.Service;

/// <summary>
/// 履歴の 1 点。生データでは min・max・avg がすべて同じ値で、count は 1。IPC でもこの形のまま送る。
/// </summary>
/// <param name="TimestampUtc">時刻（集計では区間の始まり）。</param>
/// <param name="Min">最小値。</param>
/// <param name="Max">最大値。</param>
/// <param name="Average">平均値。</param>
/// <param name="Count">集計した生データの点の数。</param>
public sealed record SensorHistoryPoint(DateTimeOffset TimestampUtc, double Min, double Max, double Average, uint Count);

namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// パイプで送る <see cref="Service.SensorHistoryPoint"/>。項目は同じ。
/// </summary>
/// <param name="TimestampUtc">時刻（集計では区間の始まり）。</param>
/// <param name="Min">最小値。</param>
/// <param name="Max">最大値。</param>
/// <param name="Average">平均値。</param>
/// <param name="Count">集計した生データの点の数。</param>
public sealed record SensorHistoryPointDto(DateTimeOffset TimestampUtc, double Min, double Max, double Average, uint Count);

namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// 異常の番号だけを渡す依頼（<see cref="SensorOperation.MarkAnomalyNotified"/>）。
/// </summary>
/// <param name="AnomalyId">異常の番号。</param>
public sealed record AnomalyIdRequest(long AnomalyId);

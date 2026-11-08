namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// 「これは正常」の印を付ける・外す依頼（<see cref="SensorOperation.SetAnomalyDismissed"/>）。
/// </summary>
/// <param name="AnomalyId">異常の番号。</param>
/// <param name="Dismissed">付けるなら true、外すなら false。</param>
public sealed record AnomalyDismissRequest(long AnomalyId, bool Dismissed);

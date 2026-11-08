namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// 異常の一覧の取得の依頼（<see cref="SensorOperation.GetAnomalies"/>）。
/// </summary>
/// <param name="FromUtc">期間の始まり（含む）。</param>
/// <param name="ToUtc">期間の終わり（含まない）。</param>
/// <param name="IncludeDismissed">「これは正常」とした異常も含めるなら true。</param>
/// <param name="Limit">取得する最大の件数。</param>
public sealed record AnomaliesRequest(DateTimeOffset FromUtc, DateTimeOffset ToUtc, bool IncludeDismissed, int Limit);

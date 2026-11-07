namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// 記録の設定の取得の返事。
/// </summary>
/// <param name="Policy">記録の設定。マスタにないか、記録が使えない場合は null。</param>
public sealed record LogPolicyResponse(SensorLogPolicyDto? Policy);

namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// 記録の設定の変更の依頼。
/// </summary>
/// <param name="Key">センサーのキーの文字列表記。</param>
/// <param name="Policy">新しい設定。</param>
public sealed record SetLogPolicyRequest(string Key, SensorLogPolicyDto Policy);

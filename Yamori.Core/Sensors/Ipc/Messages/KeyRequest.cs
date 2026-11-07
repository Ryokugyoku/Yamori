namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// センサーを 1 つ指定する依頼（記録の設定の取得）。
/// </summary>
/// <param name="Key">センサーのキーの文字列表記。</param>
public sealed record KeyRequest(string Key);

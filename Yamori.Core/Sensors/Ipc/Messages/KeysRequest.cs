namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// 複数のセンサーを指定する依頼（最新値の取得）。
/// </summary>
/// <param name="Keys">センサーのキーの文字列表記。</param>
public sealed record KeysRequest(IReadOnlyList<string> Keys);

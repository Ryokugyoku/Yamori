namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// 直近の履歴の取得の依頼。
/// </summary>
/// <param name="Key">センサーのキーの文字列表記。</param>
/// <param name="WindowMilliseconds">今からさかのぼる長さ（ミリ秒）。</param>
public sealed record RecentRequest(string Key, long WindowMilliseconds);

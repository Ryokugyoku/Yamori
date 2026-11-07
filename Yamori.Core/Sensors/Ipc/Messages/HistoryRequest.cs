using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// 時系列の履歴の取得の依頼。
/// </summary>
/// <param name="Key">センサーのキーの文字列表記。</param>
/// <param name="FromUtc">範囲の始まり（含む）。</param>
/// <param name="ToUtc">範囲の終わり（含まない）。</param>
/// <param name="Resolution">解像度。</param>
public sealed record HistoryRequest(string Key, DateTimeOffset FromUtc, DateTimeOffset ToUtc, TimeSeriesResolution Resolution);

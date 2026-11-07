namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// パイプで送る <see cref="SensorReading"/>。項目は同じで、キーは文字列表記にする。
/// </summary>
/// <param name="Key">センサーのキーの文字列表記。</param>
/// <param name="Value">値（基準単位）。読めなかった場合は null。</param>
/// <param name="TimestampUtc">読んだ時刻（UTC）。</param>
/// <param name="SourceProviderId">値を採用した Provider の ID。</param>
/// <param name="Status">読んだ結果の状態。</param>
public sealed record SensorReadingDto(string Key, double? Value, DateTimeOffset TimestampUtc, string SourceProviderId, SensorStatus Status);

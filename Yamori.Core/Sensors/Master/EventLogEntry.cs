namespace Yamori.Core.Sensors.Master;

/// <summary>
/// マスタの <c>event_log</c> の 1 行（アプリや Helper のイベント）。
/// </summary>
/// <param name="EventId">行の番号。書き込む前は 0。</param>
/// <param name="TimestampUtc">起きた時刻。</param>
/// <param name="Origin">起きた場所（例：<c>app</c>、<c>helper</c>、<c>registry</c>）。</param>
/// <param name="Level">重さ。</param>
/// <param name="Message">内容。</param>
/// <param name="DataJson">付加情報（JSON）。なければ null。</param>
public sealed record EventLogEntry(
    long EventId,
    DateTimeOffset TimestampUtc,
    string Origin,
    EventLevel Level,
    string Message,
    string? DataJson);

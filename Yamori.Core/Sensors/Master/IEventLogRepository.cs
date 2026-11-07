namespace Yamori.Core.Sensors.Master;

/// <summary>
/// マスタの <c>event_log</c>（アプリや Helper のイベント）を読み書きする。
/// </summary>
public interface IEventLogRepository
{
    /// <summary>
    /// イベントを書き込む。
    /// </summary>
    /// <param name="entry">イベント。<see cref="EventLogEntry.EventId"/> は無視する。</param>
    /// <returns>書き込んだ行（番号付き）。</returns>
    EventLogEntry Append(EventLogEntry entry);

    /// <summary>
    /// 新しいイベントから順に取得する。
    /// </summary>
    /// <param name="count">取得する最大の件数。</param>
    /// <returns>新しい順のイベント。</returns>
    IReadOnlyList<EventLogEntry> GetRecent(int count);
}

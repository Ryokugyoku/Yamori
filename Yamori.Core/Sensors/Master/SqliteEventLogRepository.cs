using Microsoft.Data.Sqlite;

namespace Yamori.Core.Sensors.Master;

/// <summary>
/// SQLite による <see cref="IEventLogRepository"/>。
/// </summary>
public sealed class SqliteEventLogRepository : IEventLogRepository
{
    private const string Columns = "event_id, ts_ms, origin, level, message, data_json";

    private readonly MasterDatabase _database;

    /// <summary>
    /// データベースを指定して作る。
    /// </summary>
    /// <param name="database">マスタのデータベース。</param>
    /// <exception cref="ArgumentNullException"><paramref name="database"/> が null。</exception>
    public SqliteEventLogRepository(MasterDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    /// <inheritdoc />
    public EventLogEntry Append(EventLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(
                connection,
                $"""
                INSERT INTO event_log (ts_ms, origin, level, message, data_json)
                VALUES ($ts, $origin, $level, $message, $data)
                RETURNING {Columns};
                """,
                ("$ts", entry.TimestampUtc.ToUnixTimeMilliseconds()),
                ("$origin", entry.Origin),
                ("$level", (int)entry.Level),
                ("$message", entry.Message),
                ("$data", entry.DataJson));
            using var reader = command.ExecuteReader();
            reader.Read();
            return Map(reader);
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<EventLogEntry> GetRecent(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        return _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(
                connection,
                $"SELECT {Columns} FROM event_log ORDER BY ts_ms DESC, event_id DESC LIMIT $count;",
                ("$count", count));
            using var reader = command.ExecuteReader();
            var result = new List<EventLogEntry>();
            while (reader.Read())
            {
                result.Add(Map(reader));
            }

            return result;
        });
    }

    private static EventLogEntry Map(SqliteDataReader reader) =>
        new(
            reader.GetInt64(0),
            DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(1)),
            reader.GetString(2),
            (EventLevel)reader.GetInt32(3),
            reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5));
}

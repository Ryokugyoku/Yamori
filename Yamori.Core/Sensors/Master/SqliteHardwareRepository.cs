using Microsoft.Data.Sqlite;

namespace Yamori.Core.Sensors.Master;

/// <summary>
/// SQLite による <see cref="IHardwareRepository"/>。
/// </summary>
public sealed class SqliteHardwareRepository : IHardwareRepository
{
    private const string Columns = "hardware_id, stable_key, kind, name, first_seen, last_seen";

    private readonly MasterDatabase _database;

    /// <summary>
    /// データベースを指定して作る。
    /// </summary>
    /// <param name="database">マスタのデータベース。</param>
    /// <exception cref="ArgumentNullException"><paramref name="database"/> が null。</exception>
    public SqliteHardwareRepository(MasterDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    /// <inheritdoc />
    public HardwareRecord Upsert(string stableKey, HardwareKind kind, string name, DateTimeOffset seenAtUtc)
    {
        ArgumentException.ThrowIfNullOrEmpty(stableKey);
        ArgumentNullException.ThrowIfNull(name);

        return _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(
                connection,
                $"""
                INSERT INTO hardware (stable_key, kind, name, first_seen, last_seen)
                VALUES ($key, $kind, $name, $seen, $seen)
                ON CONFLICT(stable_key) DO UPDATE SET name = excluded.name, last_seen = excluded.last_seen
                RETURNING {Columns};
                """,
                ("$key", stableKey),
                ("$kind", (int)kind),
                ("$name", name),
                ("$seen", seenAtUtc.ToUnixTimeMilliseconds()));
            using var reader = command.ExecuteReader();
            reader.Read();
            return Map(reader);
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<HardwareRecord> GetAll() =>
        _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(connection, $"SELECT {Columns} FROM hardware ORDER BY hardware_id;");
            using var reader = command.ExecuteReader();
            var result = new List<HardwareRecord>();
            while (reader.Read())
            {
                result.Add(Map(reader));
            }

            return result;
        });

    private static HardwareRecord Map(SqliteDataReader reader) =>
        new(
            reader.GetInt64(0),
            reader.GetString(1),
            (HardwareKind)reader.GetInt32(2),
            reader.GetString(3),
            DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(4)),
            DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(5)));
}

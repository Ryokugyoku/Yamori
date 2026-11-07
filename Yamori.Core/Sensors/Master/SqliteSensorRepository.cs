using Microsoft.Data.Sqlite;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Sensors.Master;

/// <summary>
/// SQLite による <see cref="ISensorRepository"/>。記録の間隔はミリ秒で保存する。
/// </summary>
public sealed class SqliteSensorRepository : ISensorRepository
{
    private const string Columns = "sensor_id, sensor_key, hardware_id, unit, name, log_enabled, log_interval, deadband";

    private readonly MasterDatabase _database;

    /// <summary>
    /// データベースを指定して作る。
    /// </summary>
    /// <param name="database">マスタのデータベース。</param>
    /// <exception cref="ArgumentNullException"><paramref name="database"/> が null。</exception>
    public SqliteSensorRepository(MasterDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    /// <inheritdoc />
    public SensorRecord Ensure(SensorDescriptor descriptor, long hardwareId, SensorLogPolicy defaultPolicy)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(defaultPolicy);

        return _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(
                connection,
                $"""
                INSERT INTO sensor (sensor_key, hardware_id, type, unit, name, log_enabled, log_interval, deadband)
                VALUES ($key, $hardware, $type, $unit, $name, $enabled, $interval, $deadband)
                ON CONFLICT(sensor_key) DO UPDATE SET
                    hardware_id = excluded.hardware_id, type = excluded.type, unit = excluded.unit, name = excluded.name
                RETURNING {Columns};
                """,
                ("$key", descriptor.Key.ToString()),
                ("$hardware", hardwareId),
                ("$type", (int)descriptor.SensorType),
                ("$unit", (int)descriptor.Unit),
                ("$name", descriptor.DisplayName),
                ("$enabled", defaultPolicy.Enabled ? 1 : 0),
                ("$interval", (long)defaultPolicy.Interval.TotalMilliseconds),
                ("$deadband", defaultPolicy.Deadband));
            return ReadSingle(command) ?? throw new InvalidOperationException("センサーを書き込めませんでした。");
        });
    }

    /// <inheritdoc />
    public SensorRecord? Find(SensorKey key) =>
        _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(
                connection,
                $"SELECT {Columns} FROM sensor WHERE sensor_key = $key;",
                ("$key", key.ToString()));
            return ReadSingle(command);
        });

    /// <inheritdoc />
    public IReadOnlyList<SensorRecord> GetAll() =>
        _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(connection, $"SELECT {Columns} FROM sensor ORDER BY sensor_id;");
            using var reader = command.ExecuteReader();
            var result = new List<SensorRecord>();
            while (reader.Read())
            {
                if (Map(reader) is { } record)
                {
                    result.Add(record);
                }
            }

            return result;
        });

    /// <inheritdoc />
    public SensorRecord? UpdateLogPolicy(SensorKey key, SensorLogPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(
                connection,
                $"""
                UPDATE sensor SET log_enabled = $enabled, log_interval = $interval, deadband = $deadband
                WHERE sensor_key = $key
                RETURNING {Columns};
                """,
                ("$key", key.ToString()),
                ("$enabled", policy.Enabled ? 1 : 0),
                ("$interval", (long)policy.Interval.TotalMilliseconds),
                ("$deadband", policy.Deadband));
            return ReadSingle(command);
        });
    }

    private static SensorRecord? ReadSingle(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    /// <summary>
    /// 1 行を読む。キーの表記を読めない行（今の版では使わない古い形式など）は null。
    /// </summary>
    private static SensorRecord? Map(SqliteDataReader reader) =>
        SensorKey.TryParse(reader.GetString(1), out var key)
            ? new SensorRecord(
                (uint)reader.GetInt64(0),
                key,
                reader.GetInt64(2),
                (SensorUnit)reader.GetInt32(3),
                reader.GetString(4),
                new SensorLogPolicy(
                    reader.GetInt64(5) != 0,
                    TimeSpan.FromMilliseconds(reader.GetInt64(6)),
                    reader.GetDouble(7)))
            : null;
}

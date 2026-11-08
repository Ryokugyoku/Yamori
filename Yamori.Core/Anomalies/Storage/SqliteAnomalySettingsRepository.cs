using Yamori.Core.Sensors.Master;

namespace Yamori.Core.Anomalies.Storage;

/// <summary>
/// SQLite（マスタ）による <see cref="IAnomalySettingsRepository"/>。
/// </summary>
public sealed class SqliteAnomalySettingsRepository : IAnomalySettingsRepository
{
    private readonly MasterDatabase _database;

    /// <summary>
    /// データベースを指定して作る。
    /// </summary>
    /// <param name="database">マスタのデータベース（スキーマの版 2 以上）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="database"/> が null。</exception>
    public SqliteAnomalySettingsRepository(MasterDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    /// <inheritdoc />
    public IReadOnlyList<AnomalySetting> GetAll()
    {
        var saved = _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(connection, "SELECT kind, notify_enabled, manual_threshold FROM anomaly_setting;");
            using var reader = command.ExecuteReader();
            var result = new Dictionary<AnomalyKind, AnomalySetting>();
            while (reader.Read())
            {
                var kind = (AnomalyKind)reader.GetInt32(0);
                result[kind] = new AnomalySetting(kind, reader.GetInt64(1) != 0, reader.IsDBNull(2) ? null : reader.GetDouble(2));
            }

            return result;
        });

        return AnomalySettingRules.Kinds
            .Select(kind => saved.TryGetValue(kind, out var setting) ? setting : AnomalySettingRules.Default(kind))
            .ToArray();
    }

    /// <inheritdoc />
    public void Save(AnomalySetting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);

        _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(
                connection,
                """
                INSERT INTO anomaly_setting (kind, notify_enabled, manual_threshold) VALUES ($kind, $notify, $threshold)
                ON CONFLICT(kind) DO UPDATE SET notify_enabled = excluded.notify_enabled, manual_threshold = excluded.manual_threshold;
                """,
                ("$kind", (int)setting.Kind),
                ("$notify", setting.NotificationsEnabled ? 1 : 0),
                ("$threshold", setting.ManualThreshold));
            return command.ExecuteNonQuery();
        });
    }
}

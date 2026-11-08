using Microsoft.Data.Sqlite;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Master;

namespace Yamori.Core.Anomalies.Storage;

/// <summary>
/// SQLite（マスタ）による <see cref="IAnomalyRepository"/>。時刻は UTC の Unix ミリ秒で保存する。
/// </summary>
public sealed class SqliteAnomalyRepository : IAnomalyRepository
{
    private const string Columns =
        "anomaly_id, kind, hardware_key, hardware_kind, hardware_name, started_ms, last_seen_ms, ended_ms, severity, evidence_json, dismissed_ms, notified_ms";

    private readonly MasterDatabase _database;

    /// <summary>
    /// データベースを指定して作る。
    /// </summary>
    /// <param name="database">マスタのデータベース（スキーマの版 2 以上）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="database"/> が null。</exception>
    public SqliteAnomalyRepository(MasterDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    /// <inheritdoc />
    public AnomalyRecord Insert(AnomalyRecord anomaly)
    {
        ArgumentNullException.ThrowIfNull(anomaly);

        return _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(
                connection,
                $"""
                INSERT INTO anomaly (kind, hardware_key, hardware_kind, hardware_name, started_ms, last_seen_ms, ended_ms, severity, evidence_json, dismissed_ms, notified_ms)
                VALUES ($kind, $hardwareKey, $hardwareKind, $hardwareName, $started, $lastSeen, $ended, $severity, $evidence, $dismissed, $notified)
                RETURNING {Columns};
                """,
                ("$kind", (int)anomaly.Kind),
                ("$hardwareKey", anomaly.HardwareKey),
                ("$hardwareKind", (int)anomaly.HardwareKind),
                ("$hardwareName", anomaly.HardwareName),
                ("$started", anomaly.StartedUtc.ToUnixTimeMilliseconds()),
                ("$lastSeen", anomaly.LastSeenUtc.ToUnixTimeMilliseconds()),
                ("$ended", anomaly.EndedUtc?.ToUnixTimeMilliseconds()),
                ("$severity", (int)anomaly.Severity),
                ("$evidence", AnomalyEvidenceJson.Serialize(anomaly.Evidence)),
                ("$dismissed", anomaly.DismissedUtc?.ToUnixTimeMilliseconds()),
                ("$notified", anomaly.NotifiedUtc?.ToUnixTimeMilliseconds()));
            return ReadSingle(command) ?? throw new InvalidOperationException("異常を書き込めませんでした。");
        });
    }

    /// <inheritdoc />
    public void UpdateOngoing(long anomalyId, DateTimeOffset lastSeenUtc, AnomalySeverity severity, IReadOnlyDictionary<string, double> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        Execute(
            "UPDATE anomaly SET last_seen_ms = $lastSeen, severity = $severity, evidence_json = $evidence WHERE anomaly_id = $id AND ended_ms IS NULL;",
            ("$id", anomalyId),
            ("$lastSeen", lastSeenUtc.ToUnixTimeMilliseconds()),
            ("$severity", (int)severity),
            ("$evidence", AnomalyEvidenceJson.Serialize(evidence)));
    }

    /// <inheritdoc />
    public void End(long anomalyId, DateTimeOffset endedUtc, AnomalySeverity severity, IReadOnlyDictionary<string, double> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        Execute(
            """
            UPDATE anomaly SET last_seen_ms = $ended, ended_ms = $ended, severity = $severity, evidence_json = $evidence
            WHERE anomaly_id = $id AND ended_ms IS NULL;
            """,
            ("$id", anomalyId),
            ("$ended", endedUtc.ToUnixTimeMilliseconds()),
            ("$severity", (int)severity),
            ("$evidence", AnomalyEvidenceJson.Serialize(evidence)));
    }

    /// <inheritdoc />
    public int CloseOngoing() =>
        Execute("UPDATE anomaly SET ended_ms = last_seen_ms WHERE ended_ms IS NULL;");

    /// <inheritdoc />
    public AnomalyRecord? FindLatest(AnomalyKind kind, string hardwareKey)
    {
        ArgumentNullException.ThrowIfNull(hardwareKey);

        return _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(
                connection,
                $"""
                SELECT {Columns} FROM anomaly WHERE kind = $kind AND hardware_key = $hardwareKey
                ORDER BY started_ms DESC, anomaly_id DESC LIMIT 1;
                """,
                ("$kind", (int)kind),
                ("$hardwareKey", hardwareKey));
            return ReadSingle(command);
        });
    }

    /// <inheritdoc />
    public AnomalyRecord? Reopen(long anomalyId, DateTimeOffset lastSeenUtc, AnomalySeverity severity, IReadOnlyDictionary<string, double> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        return _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(
                connection,
                $"""
                UPDATE anomaly SET ended_ms = NULL, last_seen_ms = $lastSeen, severity = $severity, evidence_json = $evidence
                WHERE anomaly_id = $id
                RETURNING {Columns};
                """,
                ("$id", anomalyId),
                ("$lastSeen", lastSeenUtc.ToUnixTimeMilliseconds()),
                ("$severity", (int)severity),
                ("$evidence", AnomalyEvidenceJson.Serialize(evidence)));
            return ReadSingle(command);
        });
    }

    /// <inheritdoc />
    public AnomalyRecord? Find(long anomalyId) =>
        _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(connection, $"SELECT {Columns} FROM anomaly WHERE anomaly_id = $id;", ("$id", anomalyId));
            return ReadSingle(command);
        });

    /// <inheritdoc />
    public IReadOnlyList<AnomalyRecord> GetOverlapping(DateTimeOffset fromUtc, DateTimeOffset toUtc, bool includeDismissed, int limit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(limit);

        return _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(
                connection,
                $"""
                SELECT {Columns} FROM anomaly
                WHERE started_ms < $to AND COALESCE(ended_ms, last_seen_ms) >= $from
                  AND ($includeDismissed = 1 OR dismissed_ms IS NULL)
                ORDER BY started_ms DESC, anomaly_id DESC
                LIMIT $limit;
                """,
                ("$from", fromUtc.ToUnixTimeMilliseconds()),
                ("$to", toUtc.ToUnixTimeMilliseconds()),
                ("$includeDismissed", includeDismissed ? 1 : 0),
                ("$limit", limit));
            using var reader = command.ExecuteReader();
            var result = new List<AnomalyRecord>();
            while (reader.Read())
            {
                result.Add(Map(reader));
            }

            return result;
        });
    }

    /// <inheritdoc />
    public AnomalyRecord? SetDismissed(long anomalyId, DateTimeOffset? dismissedUtc) =>
        _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(
                connection,
                $"UPDATE anomaly SET dismissed_ms = $dismissed WHERE anomaly_id = $id RETURNING {Columns};",
                ("$id", anomalyId),
                ("$dismissed", dismissedUtc?.ToUnixTimeMilliseconds()));
            return ReadSingle(command);
        });

    /// <inheritdoc />
    public bool MarkNotified(long anomalyId, DateTimeOffset notifiedUtc) =>
        Execute(
            "UPDATE anomaly SET notified_ms = $notified WHERE anomaly_id = $id;",
            ("$id", anomalyId),
            ("$notified", notifiedUtc.ToUnixTimeMilliseconds())) > 0;

    /// <inheritdoc />
    public DateTimeOffset? GetLastNotified(AnomalyKind kind, string hardwareKey)
    {
        ArgumentNullException.ThrowIfNull(hardwareKey);

        return _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(
                connection,
                "SELECT MAX(notified_ms) FROM anomaly WHERE kind = $kind AND hardware_key = $hardwareKey;",
                ("$kind", (int)kind),
                ("$hardwareKey", hardwareKey));
            return command.ExecuteScalar() is long ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : (DateTimeOffset?)null;
        });
    }

    private int Execute(string sql, params (string Name, object? Value)[] parameters) =>
        _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(connection, sql, parameters);
            return command.ExecuteNonQuery();
        });

    private static AnomalyRecord? ReadSingle(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    private static AnomalyRecord Map(SqliteDataReader reader) =>
        new(
            reader.GetInt64(0),
            (AnomalyKind)reader.GetInt32(1),
            reader.GetString(2),
            (HardwareKind)reader.GetInt32(3),
            reader.GetString(4),
            DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(5)),
            DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(6)),
            ReadTime(reader, 7),
            (AnomalySeverity)reader.GetInt32(8),
            AnomalyEvidenceJson.Deserialize(reader.GetString(9)),
            ReadTime(reader, 10),
            ReadTime(reader, 11));

    private static DateTimeOffset? ReadTime(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(ordinal));
}

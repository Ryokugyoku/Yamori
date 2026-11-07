namespace Yamori.Core.Sensors.Master;

/// <summary>
/// SQLite による <see cref="ISourceRepository"/>。
/// </summary>
public sealed class SqliteSourceRepository : ISourceRepository
{
    private readonly MasterDatabase _database;

    /// <summary>
    /// データベースを指定して作る。
    /// </summary>
    /// <param name="database">マスタのデータベース。</param>
    /// <exception cref="ArgumentNullException"><paramref name="database"/> が null。</exception>
    public SqliteSourceRepository(MasterDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
    }

    /// <inheritdoc />
    public ushort GetOrCreate(string providerId)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerId);

        return _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(
                connection,
                """
                INSERT INTO source (provider_id) VALUES ($id)
                ON CONFLICT(provider_id) DO UPDATE SET provider_id = excluded.provider_id
                RETURNING source_id;
                """,
                ("$id", providerId));
            return checked((ushort)(long)command.ExecuteScalar()!);
        });
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, ushort> GetAll() =>
        _database.Execute(connection =>
        {
            using var command = SqliteCommands.Create(connection, "SELECT provider_id, source_id FROM source;");
            using var reader = command.ExecuteReader();
            var result = new Dictionary<string, ushort>(StringComparer.Ordinal);
            while (reader.Read())
            {
                result[reader.GetString(0)] = checked((ushort)reader.GetInt64(1));
            }

            return result;
        });
}

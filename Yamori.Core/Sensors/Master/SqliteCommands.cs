using Microsoft.Data.Sqlite;

namespace Yamori.Core.Sensors.Master;

/// <summary>
/// SQLite のコマンドを作る小さな補助。
/// </summary>
internal static class SqliteCommands
{
    /// <summary>
    /// パラメーター付きのコマンドを作る。
    /// </summary>
    /// <param name="connection">接続。</param>
    /// <param name="sql">SQL。パラメーターは <c>$name</c> の形で書く。</param>
    /// <param name="parameters">パラメーターの名前（<c>$</c> を含む）と値の組。</param>
    /// <returns>新しいコマンド。使い終わったら破棄すること。</returns>
    public static SqliteCommand Create(SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command;
    }
}

using Microsoft.Data.Sqlite;

namespace Yamori.Core.Sensors.Master;

/// <summary>
/// マスタ（ハードウェア・センサー・取得元・イベント）の SQLite データベース。WAL モードで開き、開くときにスキーマを最新の版へ移行する。
/// </summary>
/// <remarks>
/// 書き込むのは 1 つのプロセスだけ（シングルライター）。接続は 1 本を共有し、<see cref="Execute{T}"/> で 1 つずつ使う。スレッドセーフ。
/// </remarks>
public sealed class MasterDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Lock _lock = new();

    private MasterDatabase(SqliteConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// データベースのファイルを開く。なければ作る。
    /// </summary>
    /// <param name="path">ファイルのパス。<c>:memory:</c> ならメモリの中に作る（テスト用）。</param>
    /// <returns>開いたデータベース。使い終わったら破棄すること。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が null か空。</exception>
    /// <exception cref="NotSupportedException">ファイルのスキーマが、このコードより新しい版。</exception>
    /// <exception cref="SqliteException">開けなかった。</exception>
    public static MasterDatabase Open(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (path != ":memory:")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        }

        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        }.ToString());

        try
        {
            connection.Open();
            ExecuteNonQuery(connection, "PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON; PRAGMA synchronous = NORMAL;");
            Migrate(connection);
            return new MasterDatabase(connection);
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    /// <summary>スキーマの版（<c>PRAGMA user_version</c>）を取得する。</summary>
    public int SchemaVersion => Execute(ReadVersion);

    /// <summary>
    /// 接続を使って処理を行う。ほかのスレッドと同時には使わない。
    /// </summary>
    /// <typeparam name="T">処理の結果の型。</typeparam>
    /// <param name="action">接続を使う処理。</param>
    /// <returns>処理の結果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> が null。</exception>
    public T Execute<T>(Func<SqliteConnection, T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        lock (_lock)
        {
            return action(_connection);
        }
    }

    /// <summary>
    /// 接続を閉じる。
    /// </summary>
    public void Dispose() => _connection.Dispose();

    private static void Migrate(SqliteConnection connection)
    {
        var version = ReadVersion(connection);
        if (version > MasterSchema.CurrentVersion)
        {
            throw new NotSupportedException(
                $"マスタのスキーマ（版 {version}）は、このアプリが扱える版（{MasterSchema.CurrentVersion}）より新しいため開けません。");
        }

        for (var next = version; next < MasterSchema.CurrentVersion; next++)
        {
            using var transaction = connection.BeginTransaction();
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = MasterSchema.Migrations[next] + $"\nPRAGMA user_version = {next + 1};";
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }

    private static int ReadVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void ExecuteNonQuery(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}

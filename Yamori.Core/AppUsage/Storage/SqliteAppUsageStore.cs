using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Yamori.Core.AppUsage.Storage;

/// <summary>
/// アプリごとの CPU 使用率の 1 分ごとの記録を、SQLite のファイルに保存する。WAL モードで開き、開くときにスキーマを最新の版へ移行する。
/// </summary>
/// <remarks>
/// 書き込むのは Helper だけ（シングルライター）。接続は 1 本を共有し、ロックで 1 つずつ使う。スレッドセーフ。
/// </remarks>
public sealed class SqliteAppUsageStore : IAppUsageStore, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Lock _lock = new();
    private readonly Dictionary<string, long> _appIds = new(StringComparer.OrdinalIgnoreCase);

    private SqliteAppUsageStore(SqliteConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// データベースのファイルを開く。なければ作る。
    /// </summary>
    /// <param name="path">ファイルのパス。<c>:memory:</c> ならメモリの中に作る（テスト用）。</param>
    /// <returns>開いたストア。使い終わったら破棄すること。</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> が null か空。</exception>
    /// <exception cref="NotSupportedException">ファイルのスキーマが、このコードより新しい版。</exception>
    /// <exception cref="SqliteException">開けなかった。</exception>
    public static SqliteAppUsageStore Open(string path)
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
            ExecuteNonQuery(connection, null, "PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON; PRAGMA synchronous = NORMAL;");
            Migrate(connection);
            return new SqliteAppUsageStore(connection);
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public void WriteMinute(AppCpuMinute minute)
    {
        ArgumentNullException.ThrowIfNull(minute);
        if (minute.SampleCount <= 0)
        {
            return;
        }

        var minuteMs = minute.MinuteStartUtc.ToUnixTimeMilliseconds();
        lock (_lock)
        {
            try
            {
                WriteMinuteCore(minuteMs, minute);
            }
            catch
            {
                // 取り消された書き込みで覚えたアプリの番号を使わないようにする。
                _appIds.Clear();
                throw;
            }
        }
    }

    private void WriteMinuteCore(long minuteMs, AppCpuMinute minute)
    {
        {
            using var transaction = _connection.BeginTransaction();
            var merged = MergeWithExisting(transaction, minuteMs, minute);

            using (var upsert = Command(transaction, "INSERT OR REPLACE INTO cpu_minute (minute_ms, total_percent, sample_count) VALUES ($m, $t, $c);"))
            {
                upsert.Parameters.AddWithValue("$m", minuteMs);
                upsert.Parameters.AddWithValue("$t", merged.TotalPercent);
                upsert.Parameters.AddWithValue("$c", merged.SampleCount);
                upsert.ExecuteNonQuery();
            }

            using (var clear = Command(transaction, "DELETE FROM app_cpu_minute WHERE minute_ms = $m;"))
            {
                clear.Parameters.AddWithValue("$m", minuteMs);
                clear.ExecuteNonQuery();
            }

            using (var insert = Command(transaction, "INSERT INTO app_cpu_minute (minute_ms, app_id, cpu_percent) VALUES ($m, $a, $p);"))
            {
                var app = insert.Parameters.Add("$a", SqliteType.Integer);
                var percent = insert.Parameters.Add("$p", SqliteType.Real);
                insert.Parameters.AddWithValue("$m", minuteMs);
                foreach (var share in merged.Apps)
                {
                    app.Value = GetOrAddAppId(transaction, share.Name);
                    percent.Value = share.CpuPercent;
                    insert.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }
    }

    /// <inheritdoc />
    public void DeleteBefore(DateTimeOffset cutoffUtc)
    {
        var cutoffMs = cutoffUtc.ToUnixTimeMilliseconds();
        lock (_lock)
        {
            using var transaction = _connection.BeginTransaction();
            using (var minutes = Command(transaction, "DELETE FROM app_cpu_minute WHERE minute_ms < $c; DELETE FROM cpu_minute WHERE minute_ms < $c;"))
            {
                minutes.Parameters.AddWithValue("$c", cutoffMs);
                minutes.ExecuteNonQuery();
            }

            ExecuteNonQuery(
                _connection,
                transaction,
                "DELETE FROM app WHERE NOT EXISTS (SELECT 1 FROM app_cpu_minute m WHERE m.app_id = app.app_id);");
            transaction.Commit();
            _appIds.Clear();
        }
    }

    /// <inheritdoc />
    public AppCpuTotals ReadTotals(DateTimeOffset fromUtc, DateTimeOffset toUtc, double highLoadThresholdPercent)
    {
        var fromMs = fromUtc.ToUnixTimeMilliseconds();
        var toMs = toUtc.ToUnixTimeMilliseconds();
        if (toMs <= fromMs)
        {
            return AppCpuTotals.Empty;
        }

        lock (_lock)
        {
            int observed;
            int highLoad;
            using (var counts = Command(
                null,
                "SELECT COUNT(*), COALESCE(SUM(CASE WHEN total_percent > $th THEN 1 ELSE 0 END), 0) FROM cpu_minute WHERE minute_ms >= $f AND minute_ms < $t;"))
            {
                AddRange(counts, fromMs, toMs, highLoadThresholdPercent);
                using var reader = counts.ExecuteReader();
                reader.Read();
                observed = reader.GetInt32(0);
                highLoad = reader.GetInt32(1);
            }

            if (observed == 0)
            {
                return AppCpuTotals.Empty;
            }

            var apps = new List<AppCpuTotal>();
            using (var sums = Command(
                null,
                """
                SELECT a.name,
                       SUM(m.cpu_percent),
                       SUM(CASE WHEN c.total_percent > $th THEN m.cpu_percent ELSE 0 END)
                FROM app_cpu_minute m
                JOIN cpu_minute c ON c.minute_ms = m.minute_ms
                JOIN app a ON a.app_id = m.app_id
                WHERE m.minute_ms >= $f AND m.minute_ms < $t
                GROUP BY m.app_id;
                """))
            {
                AddRange(sums, fromMs, toMs, highLoadThresholdPercent);
                using var reader = sums.ExecuteReader();
                while (reader.Read())
                {
                    apps.Add(new AppCpuTotal(reader.GetString(0), reader.GetDouble(1), reader.GetDouble(2)));
                }
            }

            return new AppCpuTotals(observed, highLoad, apps);
        }
    }

    /// <summary>
    /// 接続を閉じる。
    /// </summary>
    public void Dispose() => _connection.Dispose();

    private static void AddRange(SqliteCommand command, long fromMs, long toMs, double threshold)
    {
        command.Parameters.AddWithValue("$f", fromMs);
        command.Parameters.AddWithValue("$t", toMs);
        command.Parameters.AddWithValue("$th", threshold);
    }

    /// <summary>
    /// 同じ 1 分の記録がすでにあれば、測定の回数で重みをつけて合わせる。アプリが片方にしかなければ、もう片方では 0% とみなす。
    /// </summary>
    private AppCpuMinute MergeWithExisting(SqliteTransaction transaction, long minuteMs, AppCpuMinute minute)
    {
        double existingTotal;
        int existingCount;
        using (var select = Command(transaction, "SELECT total_percent, sample_count FROM cpu_minute WHERE minute_ms = $m;"))
        {
            select.Parameters.AddWithValue("$m", minuteMs);
            using var reader = select.ExecuteReader();
            if (!reader.Read())
            {
                return minute;
            }

            existingTotal = reader.GetDouble(0);
            existingCount = reader.GetInt32(1);
        }

        var sums = new Dictionary<string, (string Name, double Sum)>(StringComparer.OrdinalIgnoreCase);
        using (var select = Command(
            transaction,
            "SELECT a.name, m.cpu_percent FROM app_cpu_minute m JOIN app a ON a.app_id = m.app_id WHERE m.minute_ms = $m;"))
        {
            select.Parameters.AddWithValue("$m", minuteMs);
            using var reader = select.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(0);
                sums[name] = (name, reader.GetDouble(1) * existingCount);
            }
        }

        foreach (var share in minute.Apps)
        {
            var added = share.CpuPercent * minute.SampleCount;
            sums[share.Name] = sums.TryGetValue(share.Name, out var current) ? (current.Name, current.Sum + added) : (share.Name, added);
        }

        var count = existingCount + minute.SampleCount;
        var apps = sums.Values
            .Select(s => new AppCpuShare(s.Name, s.Sum / count))
            .OrderByDescending(s => s.CpuPercent)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Take(Recording.AppCpuMinuteAccumulator.MaxAppsPerMinute)
            .ToArray();
        var total = ((existingTotal * existingCount) + (minute.TotalPercent * minute.SampleCount)) / count;
        return new AppCpuMinute(minute.MinuteStartUtc, total, count, apps);
    }

    private long GetOrAddAppId(SqliteTransaction transaction, string name)
    {
        if (_appIds.TryGetValue(name, out var cached))
        {
            return cached;
        }

        using (var insert = Command(transaction, "INSERT INTO app (name) VALUES ($n) ON CONFLICT(name) DO NOTHING;"))
        {
            insert.Parameters.AddWithValue("$n", name);
            insert.ExecuteNonQuery();
        }

        using var select = Command(transaction, "SELECT app_id FROM app WHERE name = $n;");
        select.Parameters.AddWithValue("$n", name);
        var id = Convert.ToInt64(select.ExecuteScalar(), CultureInfo.InvariantCulture);
        _appIds[name] = id;
        return id;
    }

    private SqliteCommand Command(SqliteTransaction? transaction, string sql)
    {
        var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }

    private static void Migrate(SqliteConnection connection)
    {
        var version = ReadVersion(connection);
        if (version > AppUsageSchema.CurrentVersion)
        {
            throw new NotSupportedException(
                $"アプリの使用率の記録のスキーマ（版 {version}）は、このアプリが扱える版（{AppUsageSchema.CurrentVersion}）より新しいため開けません。");
        }

        for (var next = version; next < AppUsageSchema.CurrentVersion; next++)
        {
            using var transaction = connection.BeginTransaction();
            ExecuteNonQuery(connection, transaction, AppUsageSchema.Migrations[next] + $"\nPRAGMA user_version = {next + 1};");
            transaction.Commit();
        }
    }

    private static int ReadVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static void ExecuteNonQuery(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}

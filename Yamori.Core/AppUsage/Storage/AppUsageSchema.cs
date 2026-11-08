namespace Yamori.Core.AppUsage.Storage;

/// <summary>
/// アプリごとの使用率の記録（SQLite）のスキーマの移行手順。添え字 i の SQL を実行すると、版（<c>PRAGMA user_version</c>）が i から i + 1 になる。
/// </summary>
/// <remarks>既存の移行手順は書き換えないこと。変更は新しい移行手順を末尾に足して行う。時刻は UTC の Unix ミリ秒。</remarks>
public static class AppUsageSchema
{
    /// <summary>版ごとの移行手順。</summary>
    public static IReadOnlyList<string> Migrations { get; } =
    [
        // 1: 最初の版。
        """
        CREATE TABLE app (
            app_id INTEGER PRIMARY KEY AUTOINCREMENT,
            name   TEXT    NOT NULL UNIQUE COLLATE NOCASE
        );

        CREATE TABLE cpu_minute (
            minute_ms     INTEGER PRIMARY KEY,
            total_percent REAL    NOT NULL,
            sample_count  INTEGER NOT NULL
        ) WITHOUT ROWID;

        CREATE TABLE app_cpu_minute (
            minute_ms   INTEGER NOT NULL,
            app_id      INTEGER NOT NULL REFERENCES app(app_id),
            cpu_percent REAL    NOT NULL,
            PRIMARY KEY (minute_ms, app_id)
        ) WITHOUT ROWID;

        CREATE INDEX app_cpu_minute_app ON app_cpu_minute(app_id);
        """,
    ];

    /// <summary>今の版。</summary>
    public static int CurrentVersion => Migrations.Count;
}

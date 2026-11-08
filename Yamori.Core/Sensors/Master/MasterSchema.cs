namespace Yamori.Core.Sensors.Master;

/// <summary>
/// マスタ（SQLite）のスキーマの移行手順。添え字 i の SQL を実行すると、版（<c>PRAGMA user_version</c>）が i から i + 1 になる。
/// </summary>
/// <remarks>既存の移行手順は書き換えないこと。変更は新しい移行手順を末尾に足して行う。</remarks>
public static class MasterSchema
{
    /// <summary>版ごとの移行手順。</summary>
    public static IReadOnlyList<string> Migrations { get; } =
    [
        // 1: 最初の版。
        """
        CREATE TABLE hardware (
            hardware_id INTEGER PRIMARY KEY AUTOINCREMENT,
            stable_key  TEXT    NOT NULL UNIQUE,
            kind        INTEGER NOT NULL,
            name        TEXT    NOT NULL,
            first_seen  INTEGER NOT NULL,
            last_seen   INTEGER NOT NULL
        );

        CREATE TABLE sensor (
            sensor_id    INTEGER PRIMARY KEY AUTOINCREMENT,
            sensor_key   TEXT    NOT NULL UNIQUE,
            hardware_id  INTEGER NOT NULL REFERENCES hardware(hardware_id),
            type         INTEGER NOT NULL,
            unit         INTEGER NOT NULL,
            name         TEXT    NOT NULL,
            log_enabled  INTEGER NOT NULL,
            log_interval INTEGER NOT NULL,
            deadband     REAL    NOT NULL
        );

        CREATE INDEX ix_sensor_hardware ON sensor(hardware_id);

        CREATE TABLE source (
            source_id   INTEGER PRIMARY KEY AUTOINCREMENT,
            provider_id TEXT    NOT NULL UNIQUE
        );

        CREATE TABLE event_log (
            event_id  INTEGER PRIMARY KEY AUTOINCREMENT,
            ts_ms     INTEGER NOT NULL,
            origin    TEXT    NOT NULL,
            level     INTEGER NOT NULL,
            message   TEXT    NOT NULL,
            data_json TEXT
        );

        CREATE INDEX ix_event_log_ts ON event_log(ts_ms);
        """,

        // 2: 異常の一覧と、異常の設定（しきい値・通知のオン・オフ）。
        //    ended_ms は続いている間は NULL。last_seen_ms は最後に異常を確かめた時刻（Helper が止まったときの終わりの時刻に使う）。
        //    evidence_json は判断の理由の数値（名前と値の組）。dismissed_ms は「これは正常」とした時刻、notified_ms は通知を出した時刻。
        //    manual_threshold は NULL なら自動（TjMax などから決める）。
        """
        CREATE TABLE anomaly (
            anomaly_id    INTEGER PRIMARY KEY AUTOINCREMENT,
            kind          INTEGER NOT NULL,
            hardware_key  TEXT    NOT NULL,
            hardware_kind INTEGER NOT NULL,
            hardware_name TEXT    NOT NULL,
            started_ms    INTEGER NOT NULL,
            last_seen_ms  INTEGER NOT NULL,
            ended_ms      INTEGER,
            severity      INTEGER NOT NULL,
            evidence_json TEXT    NOT NULL,
            dismissed_ms  INTEGER,
            notified_ms   INTEGER
        );

        CREATE INDEX ix_anomaly_started ON anomaly(started_ms);
        CREATE INDEX ix_anomaly_target ON anomaly(kind, hardware_key, notified_ms);

        CREATE TABLE anomaly_setting (
            kind             INTEGER PRIMARY KEY,
            notify_enabled   INTEGER NOT NULL,
            manual_threshold REAL
        );
        """,
    ];

    /// <summary>今のコードが扱うスキーマの版を取得する。</summary>
    public static int CurrentVersion => Migrations.Count;
}

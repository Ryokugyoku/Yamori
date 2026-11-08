namespace Yamori.Core.Sensors.Ipc;

/// <summary>
/// クライアントが接続のとき（<see cref="IpcMessageKind.Hello"/>）に選ぶ、受け取りたい通知の種類。
/// 通知専用の常駐プログラムは <see cref="Anomalies"/> だけを選び、毎秒の値（<see cref="Readings"/>）を受け取らない。
/// </summary>
/// <remarks>
/// <para><see cref="All"/>（0。何も選ばない）はすべてを受け取る。版 2 までのクライアントはこの項目を送らないため、すべてを受け取る。</para>
/// <para>数値として送るため、既存の値の番号は変えないこと。追加は空いているビットに行う。</para>
/// </remarks>
[Flags]
public enum SensorNotificationTopics
{
    /// <summary>すべての通知（何も選ばない場合）。</summary>
    All = 0,

    /// <summary>毎秒の値（<see cref="SensorNotificationKind.ReadingsUpdated"/>）。</summary>
    Readings = 1 << 0,

    /// <summary>カタログの変化（<see cref="SensorNotificationKind.CatalogChanged"/>）。</summary>
    Catalog = 1 << 1,

    /// <summary>拡張センサーの状態の変化（<see cref="SensorNotificationKind.ExtendedSensorStateChanged"/>）。</summary>
    ExtendedState = 1 << 2,

    /// <summary>異常の知らせ（<see cref="SensorNotificationKind.AnomalyChanged"/>）。</summary>
    Anomalies = 1 << 3,
}

namespace Yamori.Core.Sensors.Ipc;

/// <summary>
/// サーバーからクライアントへの通知の種類。知らない種類の通知は、受け取った側で無視する。
/// </summary>
/// <remarks>
/// 数値として送るため、既存の値の番号は変えないこと。追加は末尾に行う。
/// </remarks>
public enum SensorNotificationKind
{
    /// <summary>新しい値を読んだ。</summary>
    ReadingsUpdated = 0,

    /// <summary>カタログが変わった。</summary>
    CatalogChanged = 1,

    /// <summary>拡張センサーの状態が変わった。</summary>
    ExtendedSensorStateChanged = 2,

    /// <summary>異常が始まった・重くなった・終わった、または「これは正常」の印が変わった（版 3 から）。</summary>
    AnomalyChanged = 3,
}

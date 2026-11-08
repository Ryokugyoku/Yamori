namespace Yamori.Core.Sensors.Ipc;

/// <summary>
/// パイプで受け付ける操作。ここにない操作（任意の MSR の読み取りなど、ハードウェアに直接届く操作）は受け付けない。
/// </summary>
/// <remarks>
/// 数値として送るため、既存の値の番号は変えないこと。追加は末尾に行う。
/// </remarks>
public enum SensorOperation
{
    /// <summary>カタログの取得。</summary>
    GetCatalog = 0,

    /// <summary>最新値の取得。</summary>
    GetLatest = 1,

    /// <summary>直近の履歴（Hot ストア）の取得。</summary>
    GetRecent = 2,

    /// <summary>時系列の履歴の取得。</summary>
    GetHistory = 3,

    /// <summary>記録の設定の取得。</summary>
    GetLogPolicy = 4,

    /// <summary>記録の設定の変更（管理者だけ）。</summary>
    SetLogPolicy = 5,

    /// <summary>拡張センサーの状態の取得。</summary>
    GetExtendedSensorState = 6,

    /// <summary>PawnIO の利用の同意の取得。</summary>
    GetPawnIoConsent = 7,

    /// <summary>PawnIO の利用の同意の変更（管理者だけ）。</summary>
    SetPawnIoConsent = 8,

    /// <summary>アプリごとの CPU 使用率の記録のまとめの取得（版 2 から）。</summary>
    GetAppCpuUsage = 9,

    /// <summary>異常の一覧の取得（版 3 から）。</summary>
    GetAnomalies = 10,

    /// <summary>「これは正常」の印を付ける・外す（版 3 から。管理者でなくてもよい）。</summary>
    SetAnomalyDismissed = 11,

    /// <summary>異常の通知を出したことの知らせ（版 3 から。管理者でなくてもよい）。</summary>
    MarkAnomalyNotified = 12,

    /// <summary>異常の設定の取得（版 3 から）。</summary>
    GetAnomalySettings = 13,

    /// <summary>異常の設定の変更（版 3 から。管理者でなくてもよい）。</summary>
    SetAnomalySetting = 14,
}

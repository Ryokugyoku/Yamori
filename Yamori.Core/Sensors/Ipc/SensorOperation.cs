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
}

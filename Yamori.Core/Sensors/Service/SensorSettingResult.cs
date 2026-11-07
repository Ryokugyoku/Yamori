namespace Yamori.Core.Sensors.Service;

/// <summary>
/// 記録の設定や PawnIO の利用の同意を変えようとした結果。
/// </summary>
/// <remarks>
/// IPC で数値として送るため、既存の値の番号は変えないこと。追加は末尾に行う。
/// </remarks>
public enum SensorSettingResult
{
    /// <summary>変えた。</summary>
    Applied = 0,

    /// <summary>対象が見つからない（マスタにないセンサーなど）。</summary>
    NotFound = 1,

    /// <summary>変える権限がない（管理者として実行していない）。</summary>
    AccessDenied = 2,

    /// <summary>今は変えられない（Helper が動いていない、記録が使えないなど）。</summary>
    Unavailable = 3,
}

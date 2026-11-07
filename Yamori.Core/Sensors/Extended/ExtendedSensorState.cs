namespace Yamori.Core.Sensors.Extended;

/// <summary>
/// 拡張センサー（PawnIO 経由の低レベルなセンサー）が使えるかどうか。UI ではこの状態を 1 か所にまとめて表示する。
/// </summary>
/// <remarks>
/// IPC で数値として送るため、既存の値の番号は変えないこと。追加は末尾に行う。
/// </remarks>
public enum ExtendedSensorState
{
    /// <summary>PawnIO が入っていない。標準 API のセンサーだけを表示する。</summary>
    NotInstalled = 0,

    /// <summary>PawnIO は入っているが、読み取りに必要な Helper（サービス）が動いていない。</summary>
    HelperRequired = 1,

    /// <summary>PawnIO が公式版（署名済み）ではないため、使わない。</summary>
    UnsupportedEdition = 2,

    /// <summary>拡張センサーを使っている。</summary>
    Active = 3,

    /// <summary>使えるはずだが、今は読めない（Helper の停止、宣言の失敗など）。</summary>
    Unavailable = 4,
}

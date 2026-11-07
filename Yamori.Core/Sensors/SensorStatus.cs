namespace Yamori.Core.Sensors;

/// <summary>
/// センサーの値を読んだ結果の状態。
/// </summary>
/// <remarks>
/// IPC で数値として送るため、既存の値の番号は変えないこと。追加は末尾に行う。
/// </remarks>
public enum SensorStatus
{
    /// <summary>値を読めた。</summary>
    Ok = 0,

    /// <summary>この環境では提供していない（カタログにない）。UI には表示しない。</summary>
    Unsupported = 1,

    /// <summary>提供しているが、今は読めない（差分待ち、Helper の停止など）。UI はレイアウトを保ったまま「—」にする。</summary>
    TemporarilyUnavailable = 2,

    /// <summary>読み取りに失敗した。</summary>
    Error = 3,
}

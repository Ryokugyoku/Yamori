namespace Yamori.Core.Sensors.Ipc;

/// <summary>
/// 依頼への返事の状態。
/// </summary>
/// <remarks>
/// 数値として送るため、既存の値の番号は変えないこと。追加は末尾に行う。
/// </remarks>
public enum IpcResponseStatus
{
    /// <summary>処理した。</summary>
    Ok = 0,

    /// <summary>知らない操作（新しい版のクライアントからの依頼など）。</summary>
    UnknownOperation = 1,

    /// <summary>依頼の内容が正しくない。</summary>
    InvalidRequest = 2,

    /// <summary>処理中に失敗した。</summary>
    Failed = 3,
}

namespace Yamori.Core.Sensors.Ipc;

/// <summary>
/// パイプの上のメッセージの種類。
/// </summary>
/// <remarks>
/// 数値として送るため、既存の値の番号は変えないこと。追加は末尾に行う。
/// </remarks>
public enum IpcMessageKind
{
    /// <summary>接続してすぐにクライアントが送る、版の申告。</summary>
    Hello = 0,

    /// <summary>サーバーの版と、接続を受け入れたかどうかの返事。</summary>
    HelloReply = 1,

    /// <summary>クライアントからの操作の依頼。</summary>
    Request = 2,

    /// <summary>依頼への返事。</summary>
    Response = 3,

    /// <summary>サーバーからクライアントへの通知（最新値、カタログ、拡張センサーの状態）。</summary>
    Notification = 4,
}

using System.Text.Json;

namespace Yamori.Core.Sensors.Ipc;

/// <summary>
/// パイプの上の 1 つのメッセージ。種類（<see cref="Kind"/>）によって使う項目が変わり、使わない項目は既定値のまま送らない。
/// </summary>
/// <remarks>
/// 版がずれても動くよう、知らない項目は読み飛ばし、ない項目は既定値として扱う。
/// 列挙値は数値で送り、知らない番号もそのまま受け取る（操作と通知は、知らない番号なら断るか無視する）。
/// </remarks>
public sealed record IpcEnvelope
{
    /// <summary>メッセージの種類を取得する。</summary>
    public IpcMessageKind Kind { get; init; }

    /// <summary>依頼と返事を対応させる番号を取得する（<see cref="IpcMessageKind.Request"/> と <see cref="IpcMessageKind.Response"/>）。</summary>
    public long Id { get; init; }

    /// <summary>送り手の版を取得する（<see cref="IpcMessageKind.Hello"/> と <see cref="IpcMessageKind.HelloReply"/>）。</summary>
    public int Version { get; init; }

    /// <summary>接続を受け入れたかどうかを取得する（<see cref="IpcMessageKind.HelloReply"/>）。</summary>
    public bool Accepted { get; init; }

    /// <summary>
    /// 受け取りたい通知の種類を取得する（<see cref="IpcMessageKind.Hello"/>。版 3 から）。
    /// <see cref="SensorNotificationTopics.All"/>（送らない場合）はすべてを受け取る。
    /// </summary>
    public SensorNotificationTopics Topics { get; init; }

    /// <summary>依頼する操作を取得する（<see cref="IpcMessageKind.Request"/>）。</summary>
    public SensorOperation Operation { get; init; }

    /// <summary>通知の種類を取得する（<see cref="IpcMessageKind.Notification"/>）。</summary>
    public SensorNotificationKind Notification { get; init; }

    /// <summary>返事の状態を取得する（<see cref="IpcMessageKind.Response"/>）。</summary>
    public IpcResponseStatus Status { get; init; }

    /// <summary>本文（依頼の引数、返事の値、通知の内容）を取得する。ない場合は null。</summary>
    public JsonElement? Payload { get; init; }
}

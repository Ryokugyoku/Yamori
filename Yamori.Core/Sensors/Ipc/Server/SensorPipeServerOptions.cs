using System.IO.Pipes;

namespace Yamori.Core.Sensors.Ipc.Server;

/// <summary>
/// センサーのパイプのサーバーの設定。
/// </summary>
public sealed class SensorPipeServerOptions
{
    /// <summary>パイプの名前を取得または設定する。既定は <see cref="SensorPipeNames.Sensors"/>。</summary>
    public string PipeName { get; init; } = SensorPipeNames.Sensors;

    /// <summary>パイプのアクセス制御を取得または設定する。null なら OS の既定（テスト用）。</summary>
    public PipeSecurity? Security { get; init; } = SensorPipeSecurity.CreateDefault();

    /// <summary>同時につなげるクライアントの最大数を取得または設定する。</summary>
    public int MaxConnections { get; init; } = 16;

    /// <summary>接続してから版の申告が届くまでの最長の待ち時間を取得または設定する。</summary>
    public TimeSpan HandshakeTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>1 つのクライアントにためておける通知の最大数を取得または設定する。超えたら古い通知から捨てる。</summary>
    public int NotificationQueueCapacity { get; init; } = 64;

    /// <summary>相手が管理者かどうかの調べ方を取得または設定する。既定は <see cref="PipeClientIdentity.IsAdministrator"/>。</summary>
    public Func<NamedPipeServerStream, bool> IsAdministrator { get; init; } = PipeClientIdentity.IsAdministrator;
}

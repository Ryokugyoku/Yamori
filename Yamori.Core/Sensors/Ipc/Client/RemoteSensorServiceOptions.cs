namespace Yamori.Core.Sensors.Ipc.Client;

/// <summary>
/// <see cref="RemoteSensorService"/> の待ち時間と、受け取る通知の設定。
/// </summary>
public sealed class RemoteSensorServiceOptions
{
    /// <summary>
    /// 受け取りたい通知の種類を取得または設定する。既定はすべて。通知専用の常駐プログラムは
    /// <see cref="SensorNotificationTopics.Anomalies"/> だけにする（毎秒の値を受け取らない）。
    /// </summary>
    /// <remarks>
    /// 選ばなかった種類の通知のイベントは発生しない。つないだ直後のカタログと拡張センサーの状態の取り直しも、選んだときだけ行う。
    /// </remarks>
    public SensorNotificationTopics Topics { get; init; } = SensorNotificationTopics.All;

    /// <summary>つながらなかった、または切れたときに、つなぎ直すまでの待ち時間を取得または設定する。</summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>版が合わなかったときに、つなぎ直すまでの待ち時間を取得または設定する（Helper の更新を待つ）。</summary>
    public TimeSpan IncompatibleRetryDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>版の返事を待つ最長の時間を取得または設定する。</summary>
    public TimeSpan HandshakeTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>依頼の返事を待つ最長の時間を取得または設定する。</summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(15);
}

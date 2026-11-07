namespace Yamori.Core.Sensors.Ipc.Client;

/// <summary>
/// <see cref="RemoteSensorService"/> の待ち時間の設定。
/// </summary>
public sealed class RemoteSensorServiceOptions
{
    /// <summary>つながらなかった、または切れたときに、つなぎ直すまでの待ち時間を取得または設定する。</summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>版が合わなかったときに、つなぎ直すまでの待ち時間を取得または設定する（Helper の更新を待つ）。</summary>
    public TimeSpan IncompatibleRetryDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>版の返事を待つ最長の時間を取得または設定する。</summary>
    public TimeSpan HandshakeTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>依頼の返事を待つ最長の時間を取得または設定する。</summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(15);
}

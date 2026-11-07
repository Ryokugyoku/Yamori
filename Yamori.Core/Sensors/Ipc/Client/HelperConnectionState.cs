namespace Yamori.Core.Sensors.Ipc.Client;

/// <summary>
/// GUI から見た Helper との接続の状態。
/// </summary>
public enum HelperConnectionState
{
    /// <summary>つなごうとしている（起動した直後）。</summary>
    Connecting = 0,

    /// <summary>つながっている。</summary>
    Connected = 1,

    /// <summary>Helper が動いていない（つながらない、または切れた）。自動でつなぎ直し続ける。</summary>
    NotRunning = 2,

    /// <summary>Helper は動いているが、版が合わずにつながらない。</summary>
    Incompatible = 3,
}

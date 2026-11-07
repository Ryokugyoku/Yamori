namespace Yamori.Core.Sensors.Ipc.Client;

/// <summary>
/// GUI から見た Helper との接続の状態。お知らせの表示に使う。
/// </summary>
public interface IHelperConnection
{
    /// <summary>
    /// 接続の状態が変わったときに発生する。引数は新しい状態。UI スレッド以外から発生する。
    /// </summary>
    event EventHandler<HelperConnectionState>? ConnectionStateChanged;

    /// <summary>今の接続の状態を取得する。</summary>
    HelperConnectionState ConnectionState { get; }
}

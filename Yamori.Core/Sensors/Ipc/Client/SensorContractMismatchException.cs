namespace Yamori.Core.Sensors.Ipc.Client;

/// <summary>
/// Helper の版とやり取りできないために、接続しなかったことを表す。
/// </summary>
public sealed class SensorContractMismatchException : IOException
{
    /// <summary>
    /// 相手の版を指定して作る。
    /// </summary>
    /// <param name="peerVersion">相手（Helper）の版。</param>
    public SensorContractMismatchException(int peerVersion)
        : base($"Helper の版（{peerVersion}）とやり取りできません。")
    {
        PeerVersion = peerVersion;
    }

    /// <summary>相手（Helper）の版を取得する。</summary>
    public int PeerVersion { get; }
}

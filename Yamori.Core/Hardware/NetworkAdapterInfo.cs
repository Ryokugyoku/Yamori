namespace Yamori.Core.Hardware;

/// <summary>
/// ネットワーク アダプター 1 つの情報。個人や場所を特定できる情報（MAC アドレス、IP アドレス、SSID、ユーザーが付けた名前）は持たない。
/// </summary>
/// <param name="Name">製品名（アダプターの説明。例：Intel(R) Wi-Fi 6E AX210 160MHz）。</param>
/// <param name="ConnectionKind">接続方式。</param>
/// <param name="IsPhysical">ハードウェアのアダプターなら true。仮想アダプター（VPN、Hyper-V など）は false。</param>
/// <param name="IsConnected">ケーブルやアクセス ポイントにつながっていれば true。</param>
public sealed record NetworkAdapterInfo(
    string Name,
    NetworkConnectionKind ConnectionKind,
    bool IsPhysical,
    bool IsConnected)
{
    /// <summary>リンク速度（ビット／秒）。つながっていないか分からない場合は null。</summary>
    public ulong? LinkSpeedBitsPerSecond { get; init; }

    /// <summary>ドライバーのバージョン。</summary>
    public string? DriverVersion { get; init; }
}

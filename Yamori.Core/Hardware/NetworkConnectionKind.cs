namespace Yamori.Core.Hardware;

/// <summary>
/// ネットワーク アダプターの接続方式。
/// </summary>
public enum NetworkConnectionKind
{
    /// <summary>分からない。</summary>
    Unknown,

    /// <summary>有線 LAN（イーサネット）。</summary>
    Ethernet,

    /// <summary>無線 LAN（Wi-Fi）。</summary>
    WiFi,

    /// <summary>モバイル回線（LTE・5G など）。</summary>
    Cellular,

    /// <summary>Bluetooth。</summary>
    Bluetooth,

    /// <summary>その他の方式。</summary>
    Other,
}

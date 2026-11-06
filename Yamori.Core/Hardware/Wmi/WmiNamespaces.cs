namespace Yamori.Core.Hardware.Wmi;

/// <summary>
/// Yamori が使う WMI 名前空間の定数。
/// </summary>
public static class WmiNamespaces
{
    /// <summary>標準の Win32_* クラスがある名前空間。</summary>
    public const string Cimv2 = @"root\cimv2";

    /// <summary>MSFT_PhysicalDisk などストレージ管理クラスがある名前空間。</summary>
    public const string Storage = @"root\Microsoft\Windows\Storage";

    /// <summary>バッテリー容量などデバイス固有のクラスがある名前空間。</summary>
    public const string Wmi = @"root\WMI";
}

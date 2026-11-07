namespace Yamori.Core.Hardware;

/// <summary>
/// Yamori が扱うハードウェア部品の種類。情報はこの単位で分類して集める。
/// </summary>
public enum HardwareComponent
{
    /// <summary>CPU（プロセッサ）。内蔵 GPU を持つ場合はそれも含む。</summary>
    Cpu,

    /// <summary>マザーボード（BIOS／UEFI を含む）。</summary>
    Motherboard,

    /// <summary>メモリ（RAM）。</summary>
    Memory,

    /// <summary>GPU（グラフィックス）。CPU に内蔵されたものは <see cref="Cpu"/> に含める。</summary>
    Gpu,

    /// <summary>ストレージ（SSD／HDD などの記憶装置）。</summary>
    Storage,

    /// <summary>ファン（冷却装置）。</summary>
    Fan,

    /// <summary>電源ユニット（バッテリーと AC 電源を含む）。</summary>
    PowerSupply,

    /// <summary>ネットワーク アダプター（有線 LAN・Wi-Fi など）。</summary>
    Network,
}

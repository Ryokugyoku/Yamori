namespace Yamori.Core.Hardware;

/// <summary>
/// PC の筐体の形。SMBIOS の筐体の種類（Win32_SystemEnclosure.ChassisTypes）を大まかにまとめたもの。
/// </summary>
public enum ChassisKind
{
    /// <summary>分からない。</summary>
    Unknown,

    /// <summary>デスクトップ（タワー、省スペース型など）。</summary>
    Desktop,

    /// <summary>ノート PC。</summary>
    Laptop,

    /// <summary>2-in-1（画面が回転・着脱できるノート PC）。</summary>
    Convertible,

    /// <summary>タブレット。</summary>
    Tablet,

    /// <summary>一体型 PC。</summary>
    AllInOne,

    /// <summary>ミニ PC・スティック PC。</summary>
    MiniPc,

    /// <summary>サーバー（ラックマウント、ブレードなど）。</summary>
    Server,

    /// <summary>上のどれにも当てはまらない形。</summary>
    Other,
}

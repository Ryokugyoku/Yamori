namespace Yamori.Core.Hardware;

/// <summary>
/// 電源の方式。値は SMBIOS の System Power Supply（Type 39）の「DMTF Power Supply Type」に合わせている。
/// </summary>
public enum PowerSupplyKind
{
    /// <summary>その他。</summary>
    Other = 1,

    /// <summary>分からない。</summary>
    Unknown = 2,

    /// <summary>リニア電源。</summary>
    Linear = 3,

    /// <summary>スイッチング電源（一般的な PC 用電源ユニット）。</summary>
    Switching = 4,

    /// <summary>バッテリー。</summary>
    Battery = 5,

    /// <summary>無停電電源装置（UPS）。</summary>
    Ups = 6,

    /// <summary>コンバーター。</summary>
    Converter = 7,

    /// <summary>レギュレーター。</summary>
    Regulator = 8,
}

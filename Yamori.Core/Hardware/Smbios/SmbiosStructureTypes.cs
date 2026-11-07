namespace Yamori.Core.Hardware.Smbios;

/// <summary>
/// Yamori が読む SMBIOS 構造体の種類の番号（DMTF SMBIOS 仕様）。
/// </summary>
public static class SmbiosStructureTypes
{
    /// <summary>冷却装置（Cooling Device）。</summary>
    public const byte CoolingDevice = 27;

    /// <summary>電源ユニット（System Power Supply）。</summary>
    public const byte SystemPowerSupply = 39;

    /// <summary>テーブルの終わり（End-of-Table）。</summary>
    public const byte EndOfTable = 127;
}

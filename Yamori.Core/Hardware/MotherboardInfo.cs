namespace Yamori.Core.Hardware;

/// <summary>
/// マザーボードと、その上のファームウェア（BIOS／UEFI）の情報。
/// メーカーが値を設定していない項目（「To Be Filled By O.E.M.」など）は null になる。
/// </summary>
/// <param name="Manufacturer">マザーボードのメーカー。</param>
/// <param name="Product">マザーボードの製品名。</param>
public sealed record MotherboardInfo(string? Manufacturer, string? Product)
{
    /// <summary>マザーボードのリビジョン。</summary>
    public string? Version { get; init; }

    /// <summary>PC 本体のメーカー（メーカー製 PC の場合）。</summary>
    public string? SystemManufacturer { get; init; }

    /// <summary>PC 本体の型番（メーカー製 PC の場合）。</summary>
    public string? SystemModel { get; init; }

    /// <summary>筐体の形。</summary>
    public ChassisKind Chassis { get; init; } = ChassisKind.Unknown;

    /// <summary>BIOS／UEFI のメーカー。</summary>
    public string? BiosVendor { get; init; }

    /// <summary>BIOS／UEFI のバージョン。</summary>
    public string? BiosVersion { get; init; }

    /// <summary>BIOS／UEFI の公開日。</summary>
    public DateOnly? BiosReleaseDate { get; init; }

    /// <summary>ファームウェアが対応している SMBIOS のバージョン（例：3.4）。</summary>
    public string? SmbiosVersion { get; init; }
}

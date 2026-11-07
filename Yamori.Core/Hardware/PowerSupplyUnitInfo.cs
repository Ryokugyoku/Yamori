namespace Yamori.Core.Hardware;

/// <summary>
/// ファームウェア（SMBIOS の System Power Supply、Type 39）が報告している電源ユニット 1 台の情報。
/// メーカーが値を設定していない項目は null になる。
/// </summary>
/// <param name="Name">装置の名前、または取り付け位置。</param>
/// <param name="Manufacturer">メーカー。</param>
/// <param name="Model">型番。</param>
/// <param name="MaxPowerWatts">最大出力（W）。報告されていない場合は null。</param>
/// <param name="Kind">電源の方式。</param>
/// <param name="Status">ファームウェアが報告する状態。</param>
/// <param name="IsHotReplaceable">電源を入れたまま交換できるかどうか。</param>
public sealed record PowerSupplyUnitInfo(
    string? Name,
    string? Manufacturer,
    string? Model,
    int? MaxPowerWatts,
    PowerSupplyKind Kind,
    DeviceStatus Status,
    bool IsHotReplaceable);

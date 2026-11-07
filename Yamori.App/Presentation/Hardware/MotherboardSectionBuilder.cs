using System;
using Yamori.Core.Hardware;
using Yamori.Localization;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// マザーボードと BIOS／UEFI の表示内容を作る。
/// </summary>
public sealed class MotherboardSectionBuilder : IHardwareSectionBuilder
{
    /// <inheritdoc />
    public HardwareComponent Component => HardwareComponent.Motherboard;

    /// <inheritdoc />
    public HardwareSection Build(HardwareInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        if (inventory.Motherboard is not { } board)
        {
            return HardwareSections.Create(Component, []);
        }

        var title = HardwareSections.Join(board.Manufacturer, board.Product);
        var facts = new HardwareFactList()
            .Add("Hw_Fact_Revision", board.Version)
            .Add("Hw_Fact_PcModel", board.SystemModel is null ? null : HardwareSections.Join(board.SystemManufacturer, board.SystemModel))
            .Add("Hw_Fact_FormFactor", HardwareLabels.Chassis(board.Chassis))
            .Add("Hw_Fact_Bios", HardwareSections.Join(board.BiosVendor, board.BiosVersion))
            .Add("Hw_Fact_BiosDate", HardwareLabels.Date(board.BiosReleaseDate))
            .Add("Hw_Fact_Smbios", board.SmbiosVersion);

        var entry = new HardwareEntry(
            title.Length > 0 ? title : UiText.Get("Hw_Board_Unknown"),
            string.Empty,
            facts.ToList());

        return HardwareSections.Create(Component, [entry], "Hw_Board_Note");
    }
}

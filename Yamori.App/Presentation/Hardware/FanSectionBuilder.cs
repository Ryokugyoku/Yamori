using System;
using System.Linq;
using Yamori.Core.Hardware;
using Yamori.Localization;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// ファン（冷却装置）の表示内容を作る。回転数の実測値が出せない理由も、説明として必ず添える。
/// </summary>
public sealed class FanSectionBuilder : IHardwareSectionBuilder
{
    /// <inheritdoc />
    public HardwareComponent Component => HardwareComponent.Fan;

    /// <inheritdoc />
    public HardwareSection Build(HardwareInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        if (inventory.Fans.Count == 0)
        {
            var none = new HardwareEntry(UiText.Get("Hw_Fan_None_Title"), UiText.Get("Hw_Fan_None_Detail"), []);
            return HardwareSections.Create(Component, [none], "Hw_Fan_Note");
        }

        var entries = inventory.Fans.Select(CreateEntry).ToArray();
        return HardwareSections.Create(Component, entries, "Hw_Fan_Note");
    }

    private static HardwareEntry CreateEntry(FanInfo fan, int index)
    {
        var type = HardwareLabels.CoolingType(fan.Type);
        var facts = new HardwareFactList()
            .Add("Hw_Fact_Type", fan.Description is null ? null : type)
            .Add("Hw_Fact_Status", HardwareLabels.Status(fan.Status))
            .Add("Hw_Fact_RatedSpeed", fan.NominalSpeedRpm is { } rpm ? UiText.Format("Hw_Value_Rpm", rpm) : null);

        return new HardwareEntry(
            fan.Description ?? UiText.Format("Hw_Fan_Numbered", type, index + 1),
            string.Empty,
            facts.ToList());
    }
}

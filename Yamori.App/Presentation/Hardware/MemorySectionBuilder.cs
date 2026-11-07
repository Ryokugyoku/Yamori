using System;
using System.Collections.Generic;
using System.Linq;
using Yamori.Core.Hardware;
using Yamori.Localization;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// メモリの表示内容を作る。1 件目に合計とスロットの空き、2 件目以降にモジュールごとの情報を表示する。
/// </summary>
public sealed class MemorySectionBuilder : IHardwareSectionBuilder
{
    /// <inheritdoc />
    public HardwareComponent Component => HardwareComponent.Memory;

    /// <inheritdoc />
    public HardwareSection Build(HardwareInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        if (inventory.Memory is not { } memory)
        {
            return HardwareSections.Create(Component, []);
        }

        var entries = new List<HardwareEntry> { CreateSummary(memory) };
        entries.AddRange(memory.Modules.Select(CreateModule));
        return HardwareSections.Create(Component, entries, "Hw_Memory_Note");
    }

    private static HardwareEntry CreateSummary(MemoryInfo memory)
    {
        var facts = new HardwareFactList()
            .Add("Hw_Fact_Slots", memory.SlotCount is { } slots
                ? UiText.Format("Hw_Memory_SlotsUsed", memory.ModuleCount, slots, memory.FreeSlots ?? 0)
                : UiText.Format("Hw_Memory_Modules", memory.ModuleCount))
            .Add("Hw_Fact_MaxCapacity", HardwareLabels.Bytes(memory.MaxCapacityBytes))
            .Add("Hw_Fact_Speed", memory.SpeedMhz is { } speed ? UiText.Format("Hw_Value_Mtps", speed) : null);

        return new HardwareEntry(
            HardwareSections.Join(HardwareLabels.Bytes(memory.TotalBytes), memory.TypeName),
            UiText.Get("Hw_Memory_Total"),
            facts.ToList());
    }

    private static HardwareEntry CreateModule(MemoryModuleInfo module)
    {
        var facts = new HardwareFactList()
            .Add("Hw_Fact_Speed", Speed(module))
            .Add("Hw_Fact_Manufacturer", module.Manufacturer)
            .Add("Hw_Fact_PartNumber", module.PartNumber)
            .Add("Hw_Fact_Voltage", module.ConfiguredVoltageMillivolts is { } mv ? UiText.Format("Hw_Value_Volts", mv / 1000.0) : null);

        return new HardwareEntry(
            module.Slot,
            HardwareSections.Join(HardwareLabels.Bytes(module.CapacityBytes), module.TypeName, module.FormFactor),
            facts.ToList());
    }

    private static string? Speed(MemoryModuleInfo module)
    {
        if (module.ConfiguredSpeedMtps is not { } configured)
        {
            return module.RatedSpeedMtps is { } rated ? UiText.Format("Hw_Value_Mtps", rated) : null;
        }

        return module.RatedSpeedMtps is { } max && max > configured
            ? UiText.Format("Hw_Memory_SpeedBelowRated", configured, max)
            : UiText.Format("Hw_Value_Mtps", configured);
    }
}

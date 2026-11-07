using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Yamori.Core.Hardware;
using Yamori.Localization;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// 電源ユニットの表示内容を作る。今の電源、ファームウェアが報告する電源ユニット、バッテリーの順に表示する。
/// </summary>
public sealed class PowerSupplySectionBuilder : IHardwareSectionBuilder
{
    /// <inheritdoc />
    public HardwareComponent Component => HardwareComponent.PowerSupply;

    /// <inheritdoc />
    public HardwareSection Build(HardwareInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        var power = inventory.PowerSupply;
        var entries = new List<HardwareEntry>();
        if (power.Source != PowerSource.Unknown)
        {
            entries.Add(new HardwareEntry(HardwareLabels.Source(power.Source), UiText.Get("Hw_Power_CurrentSource"), []));
        }

        entries.AddRange(power.Units.Select(CreateUnit));
        entries.AddRange(power.Batteries.Select(CreateBattery));
        return HardwareSections.Create(Component, entries, "Hw_Power_Note");
    }

    private static HardwareEntry CreateUnit(PowerSupplyUnitInfo unit)
    {
        var facts = new HardwareFactList()
            .Add("Hw_Fact_MaxPower", unit.MaxPowerWatts is { } watts
                ? UiText.Format("Hw_Value_Watts", watts)
                : UiText.Get("Hw_Power_WattageNotReported"))
            .Add("Hw_Fact_Type", HardwareLabels.SupplyKind(unit.Kind))
            .Add("Hw_Fact_Status", HardwareLabels.Status(unit.Status))
            .Add("Hw_Fact_Manufacturer", unit.Manufacturer)
            .Add("Hw_Fact_Model", unit.Name is null ? null : unit.Model)
            .Add("Hw_Fact_HotSwap", unit.IsHotReplaceable ? UiText.Get("Common_Yes") : null);

        return new HardwareEntry(
            unit.Name ?? unit.Model ?? UiText.Get("Hw_Power_Unit"),
            UiText.Get("Hw_Power_UnitSubtitle"),
            facts.ToList());
    }

    private static HardwareEntry CreateBattery(BatteryInfo battery)
    {
        var facts = new HardwareFactList()
            .Add("Hw_Fact_Charge", battery.ChargePercent is { } charge ? UiText.Format("Hw_Value_Percent", charge) : null)
            .Add("Hw_Fact_BatteryHealth", battery.HealthRatio is { } ratio ? UiText.Format("Hw_Battery_HealthValue", ratio * 100) : null)
            .Add("Hw_Fact_CycleCount", battery.CycleCount is > 0 and var cycles ? cycles.ToString("N0", CultureInfo.CurrentUICulture) : null);

        return new HardwareEntry(battery.Name, UiText.Get("Hw_Power_Battery"), facts.ToList());
    }
}

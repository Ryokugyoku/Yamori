using System;
using System.Collections.Generic;
using Yamori.Core.Hardware;
using Yamori.Localization;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// CPU の表示内容を作る。内蔵 GPU がある場合は、CPU の一部として 2 件目に表示する。
/// </summary>
public sealed class CpuSectionBuilder : IHardwareSectionBuilder
{
    /// <inheritdoc />
    public HardwareComponent Component => HardwareComponent.Cpu;

    /// <inheritdoc />
    public HardwareSection Build(HardwareInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        var entries = new List<HardwareEntry>();
        if (inventory.Cpu is { } cpu)
        {
            entries.Add(CreateCpuEntry(cpu));
            if (cpu.IntegratedGpu is { } integrated)
            {
                entries.Add(GpuEntryFactory.Create(integrated, UiText.Get("Hw_Cpu_IntegratedGpu")));
            }
        }

        return HardwareSections.Create(Component, entries, "Hw_Cpu_Note");
    }

    private static HardwareEntry CreateCpuEntry(CpuInfo cpu)
    {
        var facts = new HardwareFactList()
            .Add("Hw_Fact_Cores", UiText.Format("Hw_Cpu_CoresThreads", cpu.PhysicalCores, cpu.LogicalProcessors))
            .Add("Hw_Fact_CoreTypes", cpu.PerformanceCores is { } p && cpu.EfficiencyCores is { } e
                ? UiText.Format("Hw_Cpu_Hybrid", p, e)
                : null)
            .Add("Hw_Fact_RatedClock", cpu.MaxClockMhz > 0 ? UiText.Format("Hw_Value_Ghz", cpu.MaxClockMhz / 1000.0) : null)
            .Add("Hw_Fact_Cache", HardwareSections.Join(
                cpu.L2CacheBytes is { } l2 ? "L2 " + HardwareLabels.Bytes(l2) : null,
                cpu.L3CacheBytes is { } l3 ? "L3 " + HardwareLabels.Bytes(l3) : null))
            .Add("Hw_Fact_Socket", cpu.Socket)
            .Add("Hw_Fact_SocketCount", cpu.SocketCount > 1 ? cpu.SocketCount.ToString(System.Globalization.CultureInfo.CurrentUICulture) : null)
            .Add("Hw_Fact_Architecture", cpu.Architecture)
            .Add("Hw_Fact_Manufacturer", cpu.Manufacturer);

        return new HardwareEntry(cpu.Name, string.Empty, facts.ToList());
    }
}

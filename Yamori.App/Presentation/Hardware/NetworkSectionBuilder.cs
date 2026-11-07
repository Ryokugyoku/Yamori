using System;
using System.Linq;
using Yamori.Core.Hardware;
using Yamori.Localization;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// ネットワーク アダプターの表示内容を作る。ハードウェアのアダプターだけを並べ、仮想アダプターは出さない。
/// </summary>
public sealed class NetworkSectionBuilder : IHardwareSectionBuilder
{
    /// <inheritdoc />
    public HardwareComponent Component => HardwareComponent.Network;

    /// <inheritdoc />
    public HardwareSection Build(HardwareInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var entries = inventory.NetworkAdapters
            .Where(a => a.IsPhysical)
            .Select(CreateEntry)
            .ToArray();
        return HardwareSections.Create(Component, entries, "Hw_Network_Note");
    }

    private static HardwareEntry CreateEntry(NetworkAdapterInfo adapter)
    {
        var facts = new HardwareFactList()
            .Add("Hw_Fact_Connection", UiText.Get(adapter.IsConnected ? "Hw_Network_Connected" : "Hw_Network_Disconnected"))
            .Add("Hw_Fact_LinkSpeed", HardwareLabels.LinkSpeed(adapter.LinkSpeedBitsPerSecond))
            .Add("Hw_Fact_Driver", adapter.DriverVersion);

        return new HardwareEntry(adapter.Name, HardwareLabels.ConnectionKind(adapter.ConnectionKind), facts.ToList());
    }
}

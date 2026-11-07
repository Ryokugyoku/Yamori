using System;
using System.Linq;
using Yamori.Core.Hardware;
using Yamori.Localization;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// GPU の表示内容を作る。CPU の内蔵 GPU は CPU の側に表示するため、ここには含めない。
/// </summary>
public sealed class GpuSectionBuilder : IHardwareSectionBuilder
{
    /// <inheritdoc />
    public HardwareComponent Component => HardwareComponent.Gpu;

    /// <inheritdoc />
    public HardwareSection Build(HardwareInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        if (inventory.Gpus.Count == 0 && inventory.Cpu?.IntegratedGpu is not null)
        {
            // GPU は CPU の内蔵分だけ。何も取れなかったのではないことを伝える。
            var onlyIntegrated = new HardwareEntry(UiText.Get("Hw_Gpu_IntegratedOnly_Title"), UiText.Get("Hw_Gpu_IntegratedOnly_Detail"), []);
            return HardwareSections.Create(Component, [onlyIntegrated], "Hw_Gpu_Note");
        }

        var entries = inventory.Gpus.Select(g => GpuEntryFactory.Create(g)).ToArray();
        return HardwareSections.Create(Component, entries, "Hw_Gpu_Note");
    }
}

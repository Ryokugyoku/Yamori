using System;
using Yamori.Core.Hardware;
using Yamori.Localization;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// GPU 1 枚分の表示内容を作る。単体の GPU と CPU の内蔵 GPU の両方で使う。
/// </summary>
public static class GpuEntryFactory
{
    /// <summary>
    /// GPU の表示内容を作る。
    /// </summary>
    /// <param name="gpu">GPU の情報。</param>
    /// <param name="subtitle">補足（例：「内蔵 GPU」）。ない場合は空文字列。</param>
    /// <returns>GPU 1 枚分の表示内容。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="gpu"/> が null。</exception>
    public static HardwareEntry Create(GpuInfo gpu, string subtitle = "")
    {
        ArgumentNullException.ThrowIfNull(gpu);

        var facts = new HardwareFactList()
            .Add("Hw_Fact_VideoMemory", VideoMemory(gpu))
            .Add("Hw_Fact_SharedMemory", HardwareLabels.Bytes(gpu.SharedMemoryBytes))
            .Add("Hw_Fact_Display", Display(gpu))
            .Add("Hw_Fact_Driver", gpu.DriverDate is { } date
                ? UiText.Format("Hw_Value_WithDate", gpu.DriverVersion, HardwareLabels.Date(date))
                : gpu.DriverVersion)
            .Add("Hw_Fact_Manufacturer", gpu.Vendor);

        return new HardwareEntry(gpu.Name, subtitle, facts.ToList());
    }

    private static string? VideoMemory(GpuInfo gpu)
    {
        var size = HardwareLabels.Bytes(gpu.DedicatedMemoryBytes);
        return size is not null && gpu.IsDedicatedMemoryCapped ? UiText.Format("Hw_Gpu_MemoryCapped", size) : size;
    }

    private static string? Display(GpuInfo gpu)
    {
        if (gpu.HorizontalResolution is not { } width || gpu.VerticalResolution is not { } height)
        {
            return null;
        }

        return gpu.RefreshRateHz is { } rate
            ? UiText.Format("Hw_Gpu_DisplayWithRate", width, height, rate)
            : UiText.Format("Hw_Gpu_Display", width, height);
    }
}

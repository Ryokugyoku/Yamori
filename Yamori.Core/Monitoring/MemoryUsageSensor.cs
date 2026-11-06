using Yamori.Core.Hardware;
using Yamori.Core.Interop;

namespace Yamori.Core.Monitoring;

/// <summary>
/// GlobalMemoryStatusEx から、物理メモリの使用率を測る。
/// </summary>
public sealed class MemoryUsageSensor : IUsageSensor
{
    /// <inheritdoc />
    public HardwareComponent Component => HardwareComponent.Memory;

    /// <inheritdoc />
    public double? ReadUsagePercent()
    {
        var status = NativeMethods.MemoryStatusEx.Create();
        if (!NativeMethods.GlobalMemoryStatusEx(ref status) || status.TotalPhys == 0)
        {
            return null;
        }

        var used = status.TotalPhys - status.AvailPhys;
        return (double)used / status.TotalPhys * 100;
    }
}

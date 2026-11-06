using Yamori.Core.Hardware;
using Yamori.Core.Interop;

namespace Yamori.Core.Monitoring;

/// <summary>
/// GetSystemTimes の差分から、システム全体の CPU 使用率を測る。
/// </summary>
/// <remarks>
/// 前回の測定との差分で計算するため、最初の呼び出しは null を返す。スレッドセーフではない。
/// </remarks>
public sealed class CpuUsageSensor : IUsageSensor
{
    private SystemCpuTimes? _previous;

    /// <inheritdoc />
    public HardwareComponent Component => HardwareComponent.Cpu;

    /// <inheritdoc />
    public double? ReadUsagePercent()
    {
        if (!NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user))
        {
            return null;
        }

        var current = new SystemCpuTimes(idle, kernel, user);
        var previous = _previous;
        _previous = current;

        return previous is { } p ? CpuUsageCalculator.Calculate(p, current) : null;
    }
}

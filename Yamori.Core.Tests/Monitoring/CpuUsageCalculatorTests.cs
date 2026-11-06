using Yamori.Core.Monitoring;

namespace Yamori.Core.Tests.Monitoring;

public class CpuUsageCalculatorTests
{
    [Fact]
    public void ComputesBusyShareFromDeltas()
    {
        // カーネル時間はアイドルを含む。合計 100 のうちアイドル 25 → 使用率 75%。
        var previous = new SystemCpuTimes(Idle: 1000, Kernel: 2000, User: 500);
        var current = new SystemCpuTimes(Idle: 1025, Kernel: 2060, User: 540);

        Assert.Equal(75, CpuUsageCalculator.Calculate(previous, current)!.Value, precision: 10);
    }

    [Fact]
    public void ReturnsNullWhenNoTimePassed()
    {
        var times = new SystemCpuTimes(1, 2, 3);

        Assert.Null(CpuUsageCalculator.Calculate(times, times));
    }
}

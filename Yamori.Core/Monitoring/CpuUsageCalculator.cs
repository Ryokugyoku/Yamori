namespace Yamori.Core.Monitoring;

/// <summary>
/// 2 回分の CPU 時間の累計値から、その間の CPU 使用率を求める。
/// </summary>
public static class CpuUsageCalculator
{
    /// <summary>
    /// 2 回の測定の間の CPU 使用率を求める。
    /// </summary>
    /// <param name="previous">前回の累計値。</param>
    /// <param name="current">今回の累計値。</param>
    /// <returns>使用率（0〜100）。経過時間がない場合は null。</returns>
    public static double? Calculate(SystemCpuTimes previous, SystemCpuTimes current)
    {
        var idle = current.Idle - previous.Idle;
        var total = (current.Kernel - previous.Kernel) + (current.User - previous.User);
        if (total <= 0)
        {
            return null;
        }

        var busy = (double)(total - idle) / total * 100;
        return Math.Clamp(busy, 0, 100);
    }
}

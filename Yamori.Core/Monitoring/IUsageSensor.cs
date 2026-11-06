using Yamori.Core.Hardware;

namespace Yamori.Core.Monitoring;

/// <summary>
/// 1 つの部品の、今の使用率を測る。
/// </summary>
public interface IUsageSensor
{
    /// <summary>
    /// 測る部品の種類を取得する。
    /// </summary>
    HardwareComponent Component { get; }

    /// <summary>
    /// 今の使用率を測る。
    /// </summary>
    /// <returns>使用率（0〜100）。まだ測れない場合（初回の差分待ちなど）は null。</returns>
    double? ReadUsagePercent();
}

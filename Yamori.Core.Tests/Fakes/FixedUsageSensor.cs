using Yamori.Core.Hardware;
using Yamori.Core.Monitoring;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// 決まった値を返す、テスト用のセンサー。
/// </summary>
/// <param name="component">部品の種類。</param>
/// <param name="value">返す使用率。null の場合は「まだ測れない」を表す。</param>
internal sealed class FixedUsageSensor(HardwareComponent component, double? value) : IUsageSensor
{
    /// <inheritdoc />
    public HardwareComponent Component => component;

    /// <inheritdoc />
    public double? ReadUsagePercent() => value;
}

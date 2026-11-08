using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Master;

namespace Yamori.Core.Anomalies.Detection.Throttling;

/// <summary>
/// GPU のスロットリングの判定の材料を読む。GPU の制限の印は Windows から読めないため、温度（ドライバーの温度の上限が分かればそれを基準に）と
/// コアのクロックの下がり方で判断する。
/// </summary>
public sealed class GpuThrottlingSignalSource : IThrottlingSignalSource
{
    /// <inheritdoc />
    public IEnumerable<ThrottlingObservation> Read(AnomalyFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        foreach (var hardwareId in frame.Keys.Where(k => k.HardwareKind == HardwareKind.Gpu).Select(k => k.HardwareId).Distinct(StringComparer.Ordinal).ToArray())
        {
            double? Value(SensorType type, string name) => frame.Value(SensorKey.Create(HardwareKind.Gpu, hardwareId, type, name));

            var temperature = Value(SensorType.Temperature, "core") ?? Value(SensorType.Temperature, "adapter");
            if (temperature is null)
            {
                continue;
            }

            yield return new ThrottlingObservation(
                HardwareRecord.StableKeyOf(SensorKey.Create(HardwareKind.Gpu, hardwareId, SensorType.Temperature, "adapter")),
                HardwareKind.Gpu,
                new ThrottlingSignals(
                    temperature,
                    Value(SensorType.Temperature, "adapter-max"),
                    null,
                    ThermalLimit: false,
                    CriticalTemperature: false,
                    PowerLimit: false,
                    CurrentLimit: false,
                    Value(SensorType.Load, "max-engine"),
                    Value(SensorType.Clock, "core")));
        }
    }
}

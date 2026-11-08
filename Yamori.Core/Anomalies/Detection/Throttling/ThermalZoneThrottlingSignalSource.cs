using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Master;

namespace Yamori.Core.Anomalies.Detection.Throttling;

/// <summary>
/// ACPI の温度センサー（温度ゾーン）のスロットリングの判定の材料を読む。Throttle Reasons の印か、
/// 熱による CPU の性能の制限（Passive Limit が 100% 未満）を熱による制限とみなす。PawnIO がない PC（AMD など）でも使える。
/// </summary>
public sealed class ThermalZoneThrottlingSignalSource : IThrottlingSignalSource
{
    private const double NoPassiveLimitPercent = 100;

    /// <inheritdoc />
    public IEnumerable<ThrottlingObservation> Read(AnomalyFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        foreach (var hardwareId in frame.Keys.Where(k => k.HardwareKind == HardwareKind.ThermalZone).Select(k => k.HardwareId).Distinct(StringComparer.Ordinal).ToArray())
        {
            double? Value(SensorType type, string name) => frame.Value(SensorKey.Create(HardwareKind.ThermalZone, hardwareId, type, name));

            var temperature = Value(SensorType.Temperature, "zone");
            var flag = Value(SensorType.Flag, "throttling");
            var passiveLimit = Value(SensorType.Level, "passive-limit");
            if (temperature is null && flag is null && passiveLimit is null)
            {
                continue;
            }

            yield return new ThrottlingObservation(
                HardwareRecord.StableKeyOf(SensorKey.Create(HardwareKind.ThermalZone, hardwareId, SensorType.Temperature, "zone")),
                HardwareKind.ThermalZone,
                new ThrottlingSignals(
                    temperature,
                    null,
                    null,
                    ThermalLimit: flag is { } f && f != 0 || passiveLimit < NoPassiveLimitPercent,
                    CriticalTemperature: false,
                    PowerLimit: false,
                    CurrentLimit: false,
                    null,
                    null));
        }
    }
}

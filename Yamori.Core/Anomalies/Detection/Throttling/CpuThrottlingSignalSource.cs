using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Master;

namespace Yamori.Core.Anomalies.Detection.Throttling;

/// <summary>
/// CPU のスロットリングの判定の材料を読む。PawnIO があれば MSR の制限の印・パッケージ温度・熱制御の作動温度・実クロックを、
/// なければ Windows の性能の制限（<c>% Performance Limit</c>）と推定クロックを使う。
/// </summary>
public sealed class CpuThrottlingSignalSource : IThrottlingSignalSource
{
    private static readonly string[] ThermalFlags = ["thermal-throttling", "cores-thermal-throttling", "prochot"];
    private static readonly string[] PowerFlags = ["power-limit", "cores-power-limit"];
    private static readonly string[] CurrentFlags = ["cores-current-limit", "cores-cross-domain-limit"];

    /// <inheritdoc />
    public IEnumerable<ThrottlingObservation> Read(AnomalyFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        foreach (var hardwareId in frame.Keys.Where(k => k.HardwareKind == HardwareKind.Cpu).Select(k => k.HardwareId).Distinct(StringComparer.Ordinal).ToArray())
        {
            double? Value(SensorType type, string name) => frame.Value(SensorKey.Create(HardwareKind.Cpu, hardwareId, type, name));
            bool AnyFlag(IEnumerable<string> names) => names.Any(n => Value(SensorType.Flag, n) is { } v && v != 0);

            var temperature = Value(SensorType.Temperature, "package");
            var performanceLimit = Value(SensorType.Level, "performance-limit");
            var signals = new ThrottlingSignals(
                temperature,
                Value(SensorType.Temperature, "throttle-target") ?? Value(SensorType.Temperature, "tjmax"),
                Value(SensorType.Temperature, "tjmax"),
                AnyFlag(ThermalFlags),
                AnyFlag(["critical-temperature"]),
                AnyFlag(PowerFlags) || performanceLimit < ThrottlingCriteria.PerformanceLimitPercent,
                AnyFlag(CurrentFlags),
                Value(SensorType.Load, "total"),
                Value(SensorType.Clock, "average-effective") ?? Value(SensorType.Clock, "average-estimated"));

            // 温度も制限の材料もなければ、判定しない。
            var hasFlags = ThermalFlags.Concat(PowerFlags).Concat(CurrentFlags).Any(n => Value(SensorType.Flag, n) is not null);
            if (temperature is not null || hasFlags || performanceLimit is not null)
            {
                yield return new ThrottlingObservation(
                    HardwareRecord.StableKeyOf(SensorKey.Create(HardwareKind.Cpu, hardwareId, SensorType.Load, "total")),
                    HardwareKind.Cpu,
                    signals);
            }
        }
    }
}

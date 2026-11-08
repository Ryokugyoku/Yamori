using Yamori.Core.Realtime;

namespace Yamori.Core.Sensors.Providers.WindowsStandard.Mappers;

/// <summary>
/// 測定結果から ACPI の温度センサー（<c>thermal-zone/{id}</c>）のセンサーを取り出す。ID は ACPI の名前（例：<c>\_TZ.TZ00</c>）から作る。
/// </summary>
/// <remarks>
/// マザーボードが報告する温度で、CPU パッケージの温度とは意味が違うため、CPU のキーには入れない。
/// </remarks>
public sealed class ThermalZoneSensorMapper : ISnapshotSensorMapper
{

    /// <inheritdoc />
    public IEnumerable<SensorSample> Map(RealtimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var samples = new List<SensorSample>();
        foreach (var zone in snapshot.Cooling.ThermalZones)
        {
            samples.AddRange(new SensorSampleBuilder(HardwareKind.ThermalZone, HardwareIds.FromPersistentId("acpi-tz:" + zone.Name), zone.Name)
                .Add(SensorType.Temperature, "zone", "Temperature", zone.TemperatureCelsius)
                .Add(SensorType.Level, "passive-limit", "Passive Limit", zone.PassiveLimitPercent)
                .Add(SensorType.Flag, "throttling", "Throttling", UnitConversions.ToFlag(zone.IsThrottling), SensorGroups.Throttling)
                .Samples);
        }

        return samples;
    }
}

using Yamori.Core.Realtime;
using Yamori.Core.Realtime.Sources;

namespace Yamori.Core.Sensors.Providers.WindowsStandard.Mappers;

/// <summary>
/// 測定結果からバッテリー（<c>battery/0</c>）のセンサーを取り出す。バッテリーが複数ある場合は合計する（電圧は 1 つのときだけ）。
/// </summary>
public sealed class BatterySensorMapper : ISnapshotSensorMapper
{
    private const string HardwareName = "Battery";

    /// <inheritdoc />
    public IEnumerable<SensorSample> Map(RealtimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var status = snapshot.Power.Status;
        var batteries = snapshot.Power.Batteries;
        if (status?.HasBattery != true && batteries.Count == 0)
        {
            return [];
        }

        return new SensorSampleBuilder(HardwareKind.Battery, HardwareIds.Singleton, HardwareName)
            .Add(SensorType.Level, "charge", "Charge Level", status?.BatteryPercent)
            .Add(SensorType.Duration, "remaining", "Remaining Time", status?.BatteryRemaining?.TotalSeconds)
            .Add(SensorType.Power, "charge-rate", "Charge Rate", UnitConversions.MilliwattsToWatts(Sum(batteries, b => b.ChargeRateMilliwatts)))
            .Add(SensorType.Power, "discharge-rate", "Discharge Rate", UnitConversions.MilliwattsToWatts(Sum(batteries, b => b.DischargeRateMilliwatts)))
            .Add(SensorType.Energy, "remaining", "Remaining Capacity", UnitConversions.MilliwattHoursToWattHours(Sum(batteries, b => b.RemainingCapacityMilliwattHours)))
            .Add(SensorType.Voltage, "battery", "Voltage", batteries.Count == 1 ? UnitConversions.MillivoltsToVolts(batteries[0].VoltageMillivolts) : null)
            .Samples;
    }

    private static double? Sum(IReadOnlyList<BatteryRate> batteries, Func<BatteryRate, int?> selector)
    {
        var values = batteries.Select(selector).Where(v => v is not null).ToArray();
        return values.Length > 0 ? values.Sum(v => (double)v!.Value) : null;
    }
}

namespace Yamori.Core.Sensors;

/// <summary>
/// 量の種類と基準単位の対応。
/// </summary>
public static class SensorUnits
{
    /// <summary>
    /// 量の種類の基準単位を取得する。
    /// </summary>
    /// <param name="type">量の種類。</param>
    /// <returns>基準単位。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> が定義されていない値。</exception>
    public static SensorUnit ForType(SensorType type) => type switch
    {
        SensorType.Load => SensorUnit.Percent,
        SensorType.Clock => SensorUnit.Hertz,
        SensorType.Temperature => SensorUnit.Celsius,
        SensorType.Power => SensorUnit.Watt,
        SensorType.Voltage => SensorUnit.Volt,
        SensorType.Current => SensorUnit.Ampere,
        SensorType.Fan => SensorUnit.Rpm,
        SensorType.Control => SensorUnit.Percent,
        SensorType.Data => SensorUnit.Byte,
        SensorType.Throughput => SensorUnit.BytePerSecond,
        SensorType.Rate => SensorUnit.PerSecond,
        SensorType.Count => SensorUnit.Count,
        SensorType.Duration => SensorUnit.Second,
        SensorType.Level => SensorUnit.Percent,
        SensorType.Energy => SensorUnit.WattHour,
        SensorType.Flag => SensorUnit.Flag,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "定義されていない量の種類です。"),
    };
}

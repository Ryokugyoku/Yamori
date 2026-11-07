namespace Yamori.Core.Realtime.Sources;

/// <summary>
/// バッテリーの今の充放電の状態。機種が報告していない値は null。
/// </summary>
/// <param name="ChargeRateMilliwatts">充電の速さ（mW）。</param>
/// <param name="DischargeRateMilliwatts">放電の速さ（mW）。バッテリーで動いているときの PC 全体の消費電力に近い。</param>
/// <param name="VoltageMillivolts">バッテリーの電圧（mV）。</param>
/// <param name="RemainingCapacityMilliwattHours">残りの容量（mWh）。</param>
public sealed record BatteryRate(
    int? ChargeRateMilliwatts,
    int? DischargeRateMilliwatts,
    int? VoltageMillivolts,
    int? RemainingCapacityMilliwattHours);

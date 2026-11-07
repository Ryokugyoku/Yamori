using Yamori.Core.Realtime.Sources;

namespace Yamori.Core.Realtime;

/// <summary>
/// 電源の今の状態。
/// </summary>
/// <param name="Status">AC 電源かバッテリーか、残量など。読めない場合は null。</param>
/// <param name="Batteries">バッテリーごとの充放電の速さなど。バッテリーがなければ空。</param>
/// <param name="Meters">電力量計ごとの消費電力。対応していなければ空。</param>
/// <param name="GpuPower">GPU ごとの消費電力の、定格（TDP）に対する割合。ドライバーが報告している GPU だけ。</param>
public sealed record PowerRealtime(
    PowerStatus? Status,
    IReadOnlyList<BatteryRate> Batteries,
    IReadOnlyList<PowerMeterReading> Meters,
    IReadOnlyList<GpuPowerReading> GpuPower);

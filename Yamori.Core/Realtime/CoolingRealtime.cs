namespace Yamori.Core.Realtime;

/// <summary>
/// ファンと温度センサーの今の状態。
/// </summary>
/// <param name="Fans">回転数を読めたファン。マザーボードのファンはドライバーなしでは読めないため、主に GPU のファン。</param>
/// <param name="ThermalZones">ACPI の温度センサー。</param>
public sealed record CoolingRealtime(IReadOnlyList<FanReading> Fans, IReadOnlyList<ThermalZoneReading> ThermalZones);

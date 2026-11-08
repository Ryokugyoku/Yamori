using Yamori.Core.Sensors;

namespace Yamori.Core.Anomalies.Detection.Throttling;

/// <summary>
/// 部品 1 つについて読んだ、スロットリングの判定の材料。
/// </summary>
/// <param name="HardwareKey">部品のキー（<c>{hardwareKind}/{hardwareId}</c>）。</param>
/// <param name="HardwareKind">部品の種類。</param>
/// <param name="Signals">判定の材料。</param>
public sealed record ThrottlingObservation(string HardwareKey, HardwareKind HardwareKind, ThrottlingSignals Signals);

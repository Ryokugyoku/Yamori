namespace Yamori.Core.Sensors.Display;

/// <summary>
/// 詳細のツリーの 1 段目（ハードウェア）。
/// </summary>
/// <param name="Key">ハードウェアを識別するキー（<c>{hardwareKind}/{hardwareId}</c>）。</param>
/// <param name="Kind">ハードウェアの種類。</param>
/// <param name="Name">表示名。</param>
/// <param name="Groups">グループ。</param>
public sealed record SensorTreeHardware(string Key, HardwareKind Kind, string Name, IReadOnlyList<SensorTreeGroup> Groups);

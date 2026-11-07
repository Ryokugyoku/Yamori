namespace Yamori.Core.Sensors.Display;

/// <summary>
/// 詳細のツリーの 2 段目（グループ）。
/// </summary>
/// <param name="Key">グループを識別するキー（<c>{hardwareKey}/{group}</c>）。</param>
/// <param name="Group">グループの名前（<see cref="SensorDescriptor.Group"/>）。</param>
/// <param name="Sensors">センサー。</param>
public sealed record SensorTreeGroup(string Key, string Group, IReadOnlyList<SensorDescriptor> Sensors);

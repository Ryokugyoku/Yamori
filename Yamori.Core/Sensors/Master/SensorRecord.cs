using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Sensors.Master;

/// <summary>
/// マスタの <c>sensor</c> の 1 行。
/// </summary>
/// <param name="SensorId">時系列のストアで使う番号（SQLite で採番する）。</param>
/// <param name="Key">センサーのキー。</param>
/// <param name="HardwareId">所属するハードウェアの行の番号。</param>
/// <param name="Unit">値の単位。</param>
/// <param name="Name">表示名。</param>
/// <param name="LogPolicy">記録の設定。</param>
public sealed record SensorRecord(
    uint SensorId,
    SensorKey Key,
    long HardwareId,
    SensorUnit Unit,
    string Name,
    SensorLogPolicy LogPolicy);

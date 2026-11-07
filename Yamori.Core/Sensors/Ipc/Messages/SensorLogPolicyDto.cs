namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// パイプで送る <see cref="TimeSeries.SensorLogPolicy"/>。間隔はミリ秒で表す。
/// </summary>
/// <param name="Enabled">記録するかどうか。</param>
/// <param name="IntervalMilliseconds">記録の最短の間隔（ミリ秒）。</param>
/// <param name="Deadband">デッドバンド（基準単位の絶対値）。</param>
public sealed record SensorLogPolicyDto(bool Enabled, long IntervalMilliseconds, double Deadband);

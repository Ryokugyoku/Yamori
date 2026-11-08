using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Anomalies.Detection;

/// <summary>
/// 規則が記録（時系列）を読むための読み方。Helper では記録の窓口（<see cref="Sensors.Persistence.SensorPersistence.GetHistory"/>）を渡す。
/// </summary>
/// <param name="key">センサーのキー。</param>
/// <param name="fromUtc">範囲の始まり（含む）。</param>
/// <param name="toUtc">範囲の終わり（含まない）。</param>
/// <param name="resolution">解像度。</param>
/// <returns>古い順の点。記録がなければ空。</returns>
public delegate IReadOnlyList<SensorHistoryPoint> AnomalyHistoryReader(SensorKey key, DateTimeOffset fromUtc, DateTimeOffset toUtc, TimeSeriesResolution resolution);

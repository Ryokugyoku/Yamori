namespace Yamori.Core.Anomalies.Detection.Throttling;

/// <summary>
/// 部品 1 つの、1 回分（1 秒）のスロットリングの判定の材料。読めなかった値は null か false。
/// </summary>
/// <param name="TemperatureCelsius">今の温度（℃）。</param>
/// <param name="ReportedLimitCelsius">部品が報告している、性能を落とし始める温度（CPU は熱制御の作動温度、GPU はドライバーの温度の上限。℃）。</param>
/// <param name="TjMaxCelsius">CPU が報告している TjMax（℃）。</param>
/// <param name="ThermalLimit">熱による制限の印（熱・PROCHOT・温度ゾーンの制限）がオン。</param>
/// <param name="CriticalTemperature">危険な温度の印がオン。</param>
/// <param name="PowerLimit">電力による制限（電力の上限の印、Windows の性能の制限）がかかっている。</param>
/// <param name="CurrentLimit">電流による制限（電流の上限、ほかのドメインにつられた制限の印）がかかっている。</param>
/// <param name="LoadPercent">今の負荷（%）。</param>
/// <param name="ClockHertz">今のクロック（Hz）。</param>
public sealed record ThrottlingSignals(
    double? TemperatureCelsius,
    double? ReportedLimitCelsius,
    double? TjMaxCelsius,
    bool ThermalLimit,
    bool CriticalTemperature,
    bool PowerLimit,
    bool CurrentLimit,
    double? LoadPercent,
    double? ClockHertz);

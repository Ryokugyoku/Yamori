using Yamori.Core.Anomalies.Thresholds;
using Yamori.Core.Sensors;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 集計値の「高温の時間」に使う温度を決める。サーマルスロットリングの判定と同じ注意の温度（<see cref="TemperatureThresholds"/>）を使う。
/// </summary>
public static class AnalysisTemperatureThreshold
{
    /// <summary>
    /// 部品が報告する上限の温度のセンサーのキーを取得する（CPU は熱制御の作動温度か TjMax、GPU はドライバーの温度の上限）。
    /// </summary>
    /// <param name="hardware">部品。</param>
    /// <returns>候補のキー（前にあるものほど優先）。上限を報告しない部品は空。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hardware"/> が null。</exception>
    public static IReadOnlyList<SensorKey> LimitKeys(AnalysisHardware hardware)
    {
        ArgumentNullException.ThrowIfNull(hardware);
        return hardware.Part switch
        {
            AnalysisPart.Cpu =>
            [
                SensorKey.Create(HardwareKind.Cpu, hardware.HardwareId, SensorType.Temperature, "throttle-target"),
                SensorKey.Create(HardwareKind.Cpu, hardware.HardwareId, SensorType.Temperature, "tjmax"),
            ],
            AnalysisPart.Gpu => [SensorKey.Create(HardwareKind.Gpu, hardware.HardwareId, SensorType.Temperature, "adapter-max")],
            _ => [],
        };
    }

    /// <summary>
    /// 注意の温度を決める。
    /// </summary>
    /// <param name="hardware">部品。</param>
    /// <param name="reportedLimitCelsius">部品が報告している上限の温度（℃）。分からなければ null。</param>
    /// <param name="manualThresholdCelsius">サーマルスロットリングの手動の注意の温度（℃）。自動なら null。ストレージには使わない（判定の対象でないため）。</param>
    /// <returns>注意の温度（℃）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hardware"/> が null。</exception>
    public static double Resolve(AnalysisHardware hardware, double? reportedLimitCelsius, double? manualThresholdCelsius)
    {
        ArgumentNullException.ThrowIfNull(hardware);
        var manual = hardware.Part is AnalysisPart.Cpu or AnalysisPart.Gpu or AnalysisPart.ThermalZone ? manualThresholdCelsius : null;
        return TemperatureThresholds.Resolve(hardware.HardwareKind, reportedLimitCelsius, manual);
    }
}

using Yamori.Core.Sensors;

namespace Yamori.Core.Anomalies.Thresholds;

/// <summary>
/// サーマルスロットリングの注意の温度を決める。手動の指定があればそれを、なければ部品が報告する上限の温度（TjMax など）の
/// <see cref="MarginBelowTjMax"/> 下を、上限が分からなければ部品の種類ごとの初期値を使う。
/// </summary>
public static class TemperatureThresholds
{
    /// <summary>上限の温度から、注意にする温度までの差（℃）。</summary>
    public const double MarginBelowTjMax = 5;

    /// <summary>
    /// 部品の種類ごとの、上限の温度が分からないときの注意の温度（℃）を取得する。
    /// </summary>
    /// <param name="kind">部品の種類。</param>
    /// <returns>注意の温度（℃）。</returns>
    public static double DefaultFor(HardwareKind kind) => kind switch
    {
        HardwareKind.Cpu => 90,
        HardwareKind.Gpu => 85,
        HardwareKind.Storage => 70,
        HardwareKind.ThermalZone => 85,
        _ => 85,
    };

    /// <summary>
    /// 注意の温度を決める。
    /// </summary>
    /// <param name="kind">部品の種類。</param>
    /// <param name="reportedLimitCelsius">部品が報告している上限の温度（TjMax など。℃）。分からなければ null。</param>
    /// <param name="manualThresholdCelsius">手動で指定した注意の温度（℃）。自動なら null。</param>
    /// <returns>注意の温度（℃）。</returns>
    public static double Resolve(HardwareKind kind, double? reportedLimitCelsius, double? manualThresholdCelsius)
    {
        if (manualThresholdCelsius is { } manual && double.IsFinite(manual))
        {
            return manual;
        }

        return reportedLimitCelsius is { } limit && double.IsFinite(limit) && limit > MarginBelowTjMax
            ? limit - MarginBelowTjMax
            : DefaultFor(kind);
    }
}

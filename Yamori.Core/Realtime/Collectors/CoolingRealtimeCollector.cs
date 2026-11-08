using Yamori.Core.Realtime.Counters;

namespace Yamori.Core.Realtime.Collectors;

/// <summary>
/// GPU のファンの回転数と、ACPI の温度センサーの値を集める。
/// </summary>
/// <remarks>
/// マザーボードにつながったファン（CPU クーラー、ケースファン）の回転数は、カーネル ドライバーなしでは Windows から読めない（基本方針 3）。
/// </remarks>
public sealed class CoolingRealtimeCollector
{
    private const double KelvinOffset = 273.15;

    private readonly IPerformanceCounterSampler _sampler;

    /// <summary>
    /// カウンターの読み出し元を指定して作る。
    /// </summary>
    /// <param name="sampler">カウンターの読み出し元。</param>
    /// <exception cref="ArgumentNullException"><paramref name="sampler"/> が null。</exception>
    public CoolingRealtimeCollector(IPerformanceCounterSampler sampler)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        _sampler = sampler;
    }

    /// <summary>
    /// ファンと温度センサーの今の状態を集める。
    /// </summary>
    /// <param name="gpus">GPU ごとの今の状態（ファンの回転数を取り出す）。</param>
    /// <returns>ファンと温度センサーの今の状態。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="gpus"/> が null。</exception>
    public CoolingRealtime Collect(IReadOnlyList<GpuRealtime> gpus)
    {
        ArgumentNullException.ThrowIfNull(gpus);

        var fans = gpus
            .Where(g => g.Telemetry?.FanRpm is not null)
            .Select(g => new FanReading(g.Name, g.Telemetry!.FanRpm!.Value, g.Telemetry.MaxFanRpm))
            .ToArray();

        return new CoolingRealtime(fans, ReadThermalZones());
    }

    private IReadOnlyList<ThermalZoneReading> ReadThermalZones()
    {
        var precise = _sampler.Read(CounterPaths.ThermalZoneHighPrecisionTemperature);
        var coarse = _sampler.Read(CounterPaths.ThermalZoneTemperature);
        var passive = _sampler.Read(CounterPaths.ThermalZonePassiveLimit);
        var reasons = _sampler.Read(CounterPaths.ThermalZoneThrottleReasons);

        return precise.Keys.Union(coarse.Keys, StringComparer.OrdinalIgnoreCase)
            .Select(name =>
            {
                // 高精度の値は 0.1 K 単位、従来の値は K 単位。
                double? kelvin = precise.TryGetValue(name, out var tenths) && tenths > 0 ? tenths / 10
                    : coarse.TryGetValue(name, out var whole) && whole > 0 ? whole
                    : null;
                return (Name: name, Kelvin: kelvin);
            })
            .Where(x => x.Kelvin is not null)
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => new ThermalZoneReading(
                x.Name,
                Math.Round(x.Kelvin!.Value - KelvinOffset, 1),
                passive.TryGetValue(x.Name, out var limit) ? limit : null,
                reasons.TryGetValue(x.Name, out var flags) && double.IsFinite(flags) && flags >= 0 ? (uint)flags : null))
            .ToArray();
    }
}

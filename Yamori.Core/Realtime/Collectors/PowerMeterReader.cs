using Yamori.Core.Realtime.Counters;

namespace Yamori.Core.Realtime.Collectors;

/// <summary>
/// 電力量計（CPU の RAPL と、ファームウェアの電力計）のパフォーマンスカウンターから、今の消費電力を読む。
/// </summary>
/// <remarks>
/// Windows 11 は Intel・AMD の CPU が持つ電力量計（RAPL）を <c>Energy Meter</c> カウンターとして公開している。
/// ドライバーの追加も管理者権限も必要ない。0 W の値は、その範囲を測っていないものとして除く。
/// </remarks>
public sealed class PowerMeterReader
{
    private const double MilliwattsPerWatt = 1000.0;

    private readonly IPerformanceCounterSampler _sampler;

    /// <summary>
    /// カウンターの読み出し元を指定して作る。
    /// </summary>
    /// <param name="sampler">カウンターの読み出し元。</param>
    /// <exception cref="ArgumentNullException"><paramref name="sampler"/> が null。</exception>
    public PowerMeterReader(IPerformanceCounterSampler sampler)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        _sampler = sampler;
    }

    /// <summary>
    /// 電力量計ごとの今の消費電力を読む。
    /// </summary>
    /// <returns>電力量計ごとの値。名前の順。対応していなければ空。</returns>
    public IReadOnlyList<PowerMeterReading> Read() =>
        _sampler.Read(CounterPaths.EnergyMeterPower)
            .Concat(_sampler.Read(CounterPaths.PowerMeterPower))
            .Where(x => !x.Key.Equals(CounterPaths.TotalInstance, StringComparison.OrdinalIgnoreCase) && x.Value > 0)
            .Select(x => new PowerMeterReading(x.Key, ClassifyDomain(x.Key), x.Value / MilliwattsPerWatt))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <summary>
    /// 電力量計の名前から、測っている範囲を判断する。
    /// </summary>
    /// <param name="name">電力量計の名前（例：<c>rapl_package0_pkg</c>）。</param>
    /// <returns>測っている範囲。</returns>
    public static PowerMeterDomain ClassifyDomain(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!name.StartsWith("rapl_", StringComparison.OrdinalIgnoreCase))
        {
            return PowerMeterDomain.Other;
        }

        var suffix = name[(name.LastIndexOf('_') + 1)..].ToLowerInvariant();
        return suffix switch
        {
            "pkg" => PowerMeterDomain.CpuPackage,
            "pp0" => PowerMeterDomain.CpuCores,
            "pp1" => PowerMeterDomain.CpuGraphics,
            "dram" => PowerMeterDomain.Memory,
            "psys" => PowerMeterDomain.Platform,
            _ => PowerMeterDomain.Other,
        };
    }
}

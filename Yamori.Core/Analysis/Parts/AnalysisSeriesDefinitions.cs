using Yamori.Core.Sensors;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 部品の種類ごとに、グラフに重ねる負荷・温度・クロックのセンサーの候補を決める。候補は前にあるものほど優先する。
/// </summary>
public static class AnalysisSeriesDefinitions
{
    private static readonly IReadOnlyDictionary<AnalysisPart, (AnalysisSeriesKind Kind, (SensorType Type, string Name)[] Candidates)[]> Definitions =
        new Dictionary<AnalysisPart, (AnalysisSeriesKind, (SensorType, string)[])[]>
        {
            [AnalysisPart.Cpu] =
            [
                (AnalysisSeriesKind.Load, [(SensorType.Load, "total")]),
                (AnalysisSeriesKind.Temperature, [(SensorType.Temperature, "package")]),
                (AnalysisSeriesKind.Clock, [(SensorType.Clock, "average-effective"), (SensorType.Clock, "average-estimated")]),
            ],
            [AnalysisPart.Gpu] =
            [
                (AnalysisSeriesKind.Load, [(SensorType.Load, "max-engine")]),
                (AnalysisSeriesKind.Temperature, [(SensorType.Temperature, "core"), (SensorType.Temperature, "adapter")]),
                (AnalysisSeriesKind.Clock, [(SensorType.Clock, "core")]),
            ],
            [AnalysisPart.Memory] =
            [
                (AnalysisSeriesKind.Load, [(SensorType.Load, "physical")]),
            ],
            [AnalysisPart.Storage] =
            [
                (AnalysisSeriesKind.Load, [(SensorType.Load, "active-time")]),
                (AnalysisSeriesKind.Temperature, [(SensorType.Temperature, "drive")]),
            ],
            [AnalysisPart.Network] =
            [
                (AnalysisSeriesKind.Load, [(SensorType.Load, "link")]),
            ],
            [AnalysisPart.ThermalZone] =
            [
                (AnalysisSeriesKind.Temperature, [(SensorType.Temperature, "zone")]),
            ],
        };

    /// <summary>
    /// 部品のグラフに重ねるセンサーを選ぶ。カタログにある候補のうち最初のものを選び、
    /// カタログにその部品のセンサーが 1 つもないとき（Helper につながっていない、今はない部品など）は最初の候補を使う。
    /// </summary>
    /// <param name="hardware">部品。</param>
    /// <param name="catalogKeys">カタログにあるセンサーのキー。</param>
    /// <returns>種類ごとのセンサー（負荷・温度・クロックの順）。その部品にない種類は含めない。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static IReadOnlyList<(AnalysisSeriesKind Kind, SensorKey Key)> Resolve(AnalysisHardware hardware, IReadOnlyCollection<SensorKey> catalogKeys)
    {
        ArgumentNullException.ThrowIfNull(hardware);
        ArgumentNullException.ThrowIfNull(catalogKeys);

        var kind = hardware.HardwareKind;
        var knowsHardware = catalogKeys.Any(k => k.HardwareKind == kind && k.HardwareId == hardware.HardwareId);
        var result = new List<(AnalysisSeriesKind, SensorKey)>();
        foreach (var (seriesKind, candidates) in Definitions[hardware.Part])
        {
            var keys = candidates.Select(c => SensorKey.Create(kind, hardware.HardwareId, c.Type, c.Name)).ToArray();
            SensorKey? chosen = knowsHardware ? keys.FirstOrDefault(catalogKeys.Contains) : keys[0];
            if (chosen is { HardwareId: not null } key)
            {
                result.Add((seriesKind, key));
            }
        }

        return result;
    }
}

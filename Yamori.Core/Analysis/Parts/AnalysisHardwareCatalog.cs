using Yamori.Core.Anomalies;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Display;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// センサーのカタログと異常の一覧から、分析できる部品を部品の種類ごとに集める。
/// </summary>
public static class AnalysisHardwareCatalog
{
    /// <summary>
    /// 部品の種類ごとの、分析できる部品を求める。
    /// </summary>
    /// <param name="catalog">センサーのカタログ。</param>
    /// <param name="anomalies">異常（今はもうない部品の異常も選べるように、その部品も加える）。</param>
    /// <returns>部品の種類ごとの部品（名前の自然な順）。部品がない種類は含めない。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static IReadOnlyDictionary<AnalysisPart, IReadOnlyList<AnalysisHardware>> Build(
        IEnumerable<SensorDescriptor> catalog,
        IEnumerable<AnomalyRecord> anomalies)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(anomalies);

        var found = new Dictionary<(AnalysisPart Part, string Id), string>();
        foreach (var descriptor in catalog)
        {
            if (AnalysisParts.From(descriptor.HardwareKind) is { } part)
            {
                found.TryAdd((part, descriptor.Key.HardwareId), descriptor.HardwareName);
            }
        }

        foreach (var anomaly in anomalies)
        {
            if (AnalysisHardware.TryParseKey(anomaly.HardwareKey, out var kind, out var id) && AnalysisParts.From(kind) is { } part)
            {
                found.TryAdd((part, id), anomaly.HardwareName);
            }
        }

        var comparer = NaturalStringComparer.Instance;
        return found
            .Select(f => new AnalysisHardware(f.Key.Part, f.Key.Id, f.Value))
            .GroupBy(h => h.Part)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<AnalysisHardware>)g.OrderBy(h => h.Name, comparer).ThenBy(h => h.HardwareId, StringComparer.Ordinal).ToArray());
    }
}

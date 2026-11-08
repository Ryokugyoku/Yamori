using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Master;

namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 今あるセンサーから、「ふだんより悪い」を判定する部品と、使うセンサーを決める。
/// CPU は全体の負荷とパッケージ温度（温度と待機中の負荷の両方を比べる）、GPU は最も使われているエンジンの負荷とコア（なければアダプター）の温度（温度だけ）。
/// </summary>
public static class UsualPartDefinitions
{
    /// <summary>
    /// 部品を決める。
    /// </summary>
    /// <param name="keys">今値のあるセンサーのキー。</param>
    /// <returns>判定する部品。負荷のセンサーがない部品は含めない。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keys"/> が null。</exception>
    public static IReadOnlyList<UsualPartDefinition> From(IEnumerable<SensorKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var set = keys.ToHashSet();
        var parts = new List<UsualPartDefinition>();
        foreach (var hardwareId in IdsOf(set, HardwareKind.Cpu))
        {
            var load = SensorKey.Create(HardwareKind.Cpu, hardwareId, SensorType.Load, "total");
            if (set.Contains(load))
            {
                parts.Add(new UsualPartDefinition(
                    HardwareRecord.StableKeyOf(load),
                    HardwareKind.Cpu,
                    load,
                    Existing(set, SensorKey.Create(HardwareKind.Cpu, hardwareId, SensorType.Temperature, "package")),
                    CompareIdleLoad: true));
            }
        }

        foreach (var hardwareId in IdsOf(set, HardwareKind.Gpu))
        {
            var load = SensorKey.Create(HardwareKind.Gpu, hardwareId, SensorType.Load, "max-engine");
            var temperature = Existing(set, SensorKey.Create(HardwareKind.Gpu, hardwareId, SensorType.Temperature, "core"))
                ?? Existing(set, SensorKey.Create(HardwareKind.Gpu, hardwareId, SensorType.Temperature, "adapter"));
            if (set.Contains(load) && temperature is not null)
            {
                parts.Add(new UsualPartDefinition(HardwareRecord.StableKeyOf(load), HardwareKind.Gpu, load, temperature, CompareIdleLoad: false));
            }
        }

        return parts;
    }

    private static IEnumerable<string> IdsOf(HashSet<SensorKey> keys, HardwareKind kind) =>
        keys.Where(k => k.HardwareKind == kind).Select(k => k.HardwareId).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);

    private static SensorKey? Existing(HashSet<SensorKey> keys, SensorKey key) => keys.Contains(key) ? key : null;
}

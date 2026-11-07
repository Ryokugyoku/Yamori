namespace Yamori.Core.Sensors.Display;

/// <summary>
/// カタログから、ハードウェア → グループ → センサーの詳細のツリーを組み立てる。取得元ごとの分岐はしない。
/// </summary>
public static class SensorTreeBuilder
{
    /// <summary>
    /// ツリーを組み立てる。表示しないセンサーは含めず、センサーが 1 つも残らないグループとハードウェアも含めない。
    /// </summary>
    /// <param name="catalog">カタログ。</param>
    /// <param name="isVisible">センサーを表示するかどうかを返す処理。</param>
    /// <returns>
    /// ハードウェアの種類の順（同じ種類は名前の順）に並べたツリー。グループは量の種類の順（<see cref="SensorType"/> の順、
    /// 種類が同じなら名前の順）、センサーは名前の数字を数として比べた順。
    /// </returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static IReadOnlyList<SensorTreeHardware> Build(IEnumerable<SensorDescriptor> catalog, Func<SensorKey, bool> isVisible)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(isVisible);

        return catalog
            .Where(d => isVisible(d.Key))
            .GroupBy(d => (d.HardwareKind, d.Key.HardwareId))
            .Select(hardware =>
            {
                var hardwareKey = SensorKeyTokens.ToToken(hardware.Key.HardwareKind) + SensorKeyTokens.Separator + hardware.Key.HardwareId;
                var groups = hardware
                    .GroupBy(d => d.Group, StringComparer.Ordinal)
                    .Select(g => new SensorTreeGroup(
                        hardwareKey + SensorKeyTokens.Separator + g.Key,
                        g.Key,
                        g.OrderBy(d => d.Key.Name, NaturalStringComparer.Instance).ToArray()))
                    .OrderBy(g => g.Sensors.Min(d => (int)d.SensorType))
                    .ThenBy(g => g.Group, StringComparer.Ordinal)
                    .ToArray();
                return new SensorTreeHardware(hardwareKey, hardware.Key.HardwareKind, hardware.First().HardwareName, groups);
            })
            .OrderBy(h => (int)h.Kind)
            .ThenBy(h => h.Name, NaturalStringComparer.Instance)
            .ThenBy(h => h.Key, StringComparer.Ordinal)
            .ToArray();
    }
}

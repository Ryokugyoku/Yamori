namespace Yamori.Core.Sensors.Display;

/// <summary>
/// 詳細のツリーのグループから、リアルタイム画面のカード（<see cref="SensorPanel"/>）を作る。
/// </summary>
public static class SensorPanelBuilder
{
    /// <summary>この数以上のセンサーがあるカードは、小さなマスで詰めて並べる。</summary>
    public const int DenseThreshold = 8;

    /// <summary>
    /// カードを作る。量の種類が混ざっているグループは種類ごとに分け、<see cref="SensorType"/> の順に並べる。
    /// </summary>
    /// <param name="group">グループ。</param>
    /// <returns>カード。グループの中の順を保つ。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="group"/> が null。</exception>
    public static IReadOnlyList<SensorPanel> Build(SensorTreeGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var byType = group.Sensors.GroupBy(s => s.SensorType).OrderBy(g => (int)g.Key).ToArray();
        if (byType.Length <= 1)
        {
            return [Create(group.Key, group.Group, null, group.Sensors)];
        }

        return byType
            .Select(t => Create(
                group.Key + SensorKeyTokens.Separator + SensorKeyTokens.ToToken(t.Key),
                group.Group,
                t.Key,
                t.ToArray()))
            .ToArray();
    }

    private static SensorPanel Create(string key, string group, SensorType? splitType, IReadOnlyList<SensorDescriptor> sensors)
    {
        var (labels, suffix) = SensorLabelShortener.StripCommonSuffix(sensors.Select(s => s.DisplayName).ToArray());
        return new SensorPanel(key, group, splitType, sensors, labels, suffix, sensors.Count >= DenseThreshold);
    }
}

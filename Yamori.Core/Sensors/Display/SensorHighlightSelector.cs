namespace Yamori.Core.Sensors.Display;

/// <summary>
/// ハードウェアのセンサーから、リアルタイム画面で大きく見せる値を選ぶ。
/// </summary>
public static class SensorHighlightSelector
{
    /// <summary>大きく見せる値の最大の数。</summary>
    public const int MaxCount = 4;

    /// <summary>
    /// 大きく見せる値を選ぶ。種類ごとの枠（<see cref="SensorHighlightDefinitions"/>）から、枠ごとに最も優先する候補を 1 つずつ選ぶ。
    /// どの枠にも当てはまらなければ、センサーの数が少ないグループの先頭から選ぶ（スレッドごとの値など、数の多いグループは使わない）。
    /// </summary>
    /// <param name="hardware">ハードウェア。</param>
    /// <returns>表示する順のセンサー。最大 <see cref="MaxCount"/> 個。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hardware"/> が null。</exception>
    public static IReadOnlyList<SensorDescriptor> Select(SensorTreeHardware hardware)
    {
        ArgumentNullException.ThrowIfNull(hardware);

        var sensors = hardware.Groups.SelectMany(g => g.Sensors).ToArray();
        var selected = new List<SensorDescriptor>();
        foreach (var slot in SensorHighlightDefinitions.For(hardware.Kind))
        {
            var best = sensors
                .Select(s => (Sensor: s, Rank: slot.RankOf(s.Key)))
                .Where(c => c.Rank >= 0)
                .OrderBy(c => c.Rank)
                .Select(c => c.Sensor)
                .FirstOrDefault();
            if (best is not null && !selected.Contains(best))
            {
                selected.Add(best);
            }
        }

        if (selected.Count > 0)
        {
            return selected.Take(MaxCount).ToArray();
        }

        return hardware.Groups
            .Where(g => g.Sensors.Count < SensorPanelBuilder.DenseThreshold)
            .Select(g => g.Sensors[0])
            .Take(MaxCount)
            .ToArray();
    }
}

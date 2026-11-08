namespace Yamori.Core.Sensors.Display;

/// <summary>
/// リアルタイム画面で、ハードウェアの種類ごとに大きく見せる値の枠の定義。
/// </summary>
/// <remarks>
/// 拡張センサー（PawnIO）の値のほうが正確なため、同じ枠では先に置く（例：実クロック → 推定のクロック）。
/// 定義のない種類と、どの枠にも当てはまらないハードウェアは、<see cref="SensorHighlightSelector"/> が各グループの先頭から選ぶ。
/// </remarks>
public static class SensorHighlightDefinitions
{
    private static readonly Dictionary<HardwareKind, IReadOnlyList<SensorHighlightSlot>> Slots = new()
    {
        [HardwareKind.Cpu] =
        [
            SensorHighlightSlot.Of("load/total", "load/processor-time"),
            SensorHighlightSlot.Of("temperature/package"),
            SensorHighlightSlot.Of("clock/average-effective", "clock/average-estimated"),
            SensorHighlightSlot.Of("power/package"),
        ],
        [HardwareKind.Memory] =
        [
            SensorHighlightSlot.Of("load/physical"),
            SensorHighlightSlot.Of("data/used"),
            SensorHighlightSlot.Of("data/available"),
            SensorHighlightSlot.Of("data/committed"),
        ],
        [HardwareKind.Gpu] =
        [
            SensorHighlightSlot.Of("load/core", "load/max-engine"),
            SensorHighlightSlot.Of("temperature/core", "temperature/adapter"),
            SensorHighlightSlot.Of("data/dedicated-used"),
            SensorHighlightSlot.Of("clock/core"),
        ],
        [HardwareKind.Storage] =
        [
            SensorHighlightSlot.Of("load/active-time"),
            SensorHighlightSlot.Of("throughput/read"),
            SensorHighlightSlot.Of("throughput/write"),
            SensorHighlightSlot.Of("temperature/drive"),
        ],
        [HardwareKind.Volume] =
        [
            SensorHighlightSlot.Of("load/used"),
            SensorHighlightSlot.Of("data/free"),
            SensorHighlightSlot.Of("data/total"),
        ],
        [HardwareKind.Network] =
        [
            SensorHighlightSlot.Of("throughput/received"),
            SensorHighlightSlot.Of("throughput/sent"),
            SensorHighlightSlot.Of("load/link"),
        ],
        [HardwareKind.Battery] =
        [
            SensorHighlightSlot.Of("level/charge"),
            SensorHighlightSlot.Of("duration/remaining"),
            SensorHighlightSlot.Of("power/discharge-rate", "power/charge-rate"),
        ],
        [HardwareKind.ThermalZone] =
        [
            SensorHighlightSlot.Of("temperature/zone"),
            SensorHighlightSlot.Of("level/passive-limit"),
        ],
        [HardwareKind.System] =
        [
            SensorHighlightSlot.Of("count/processes"),
            SensorHighlightSlot.Of("count/threads"),
            SensorHighlightSlot.Of("duration/uptime"),
            SensorHighlightSlot.Of("power/platform"),
        ],
    };

    /// <summary>
    /// ハードウェアの種類の枠を取得する。
    /// </summary>
    /// <param name="kind">ハードウェアの種類。</param>
    /// <returns>表示する順の枠。定義のない種類なら空。</returns>
    public static IReadOnlyList<SensorHighlightSlot> For(HardwareKind kind) =>
        Slots.TryGetValue(kind, out var slots) ? slots : [];
}

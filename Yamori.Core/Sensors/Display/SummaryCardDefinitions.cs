namespace Yamori.Core.Sensors.Display;

/// <summary>
/// 標準のサマリーカードの定義。表示する順に並べる。
/// </summary>
/// <remarks>
/// 候補の先頭には、PawnIO で取れる、より正確な値のキーを置く（例：CPU パッケージの温度、MSR から読む実クロック）。
/// 標準 API の値は、それより後ろの候補にする。
/// </remarks>
public static class SummaryCardDefinitions
{
    /// <summary>標準のカードの定義を取得する。</summary>
    public static IReadOnlyList<SummaryCardDefinition> Default { get; } =
    [
        SummaryCardDefinition.Create("CpuLoad", "cpu/0/load/total"),
        SummaryCardDefinition.Create("CpuTemperature", "cpu/0/temperature/package", "thermal-zone/*/temperature/zone"),
        SummaryCardDefinition.Create("CpuClock", "cpu/0/clock/average-effective", "cpu/0/clock/average-estimated"),
        SummaryCardDefinition.Create("CpuPower", "cpu/0/power/package"),
        SummaryCardDefinition.Create("MemoryLoad", "memory/0/load/physical"),
        SummaryCardDefinition.Create("GpuLoad", "gpu/*/load/core", "gpu/*/load/max-engine"),
        SummaryCardDefinition.Create("GpuTemperature", "gpu/*/temperature/core", "gpu/*/temperature/adapter"),
    ];
}

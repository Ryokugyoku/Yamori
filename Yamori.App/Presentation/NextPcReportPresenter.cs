using System;
using System.Collections.Generic;
using System.Linq;
using Yamori.Core.Formatting;
using Yamori.Core.Hardware;
using Yamori.Core.Processes;
using Yamori.Core.Recommendations;
using Yamori.Localization;

namespace Yamori.Presentation;

/// <summary>
/// <see cref="Core.Reports.NextPcReport"/> の中身を、画面に出す項目の一覧に変換する。言語は現在の UI カルチャに従う。
/// </summary>
public sealed class NextPcReportPresenter
{
    private readonly RecommendationTextFormatter _recommendationFormatter;

    /// <summary>
    /// おすすめの文章化処理を指定して作る。
    /// </summary>
    /// <param name="recommendationFormatter">おすすめを文章にする処理。</param>
    /// <exception cref="ArgumentNullException"><paramref name="recommendationFormatter"/> が null。</exception>
    public NextPcReportPresenter(RecommendationTextFormatter recommendationFormatter)
    {
        ArgumentNullException.ThrowIfNull(recommendationFormatter);
        _recommendationFormatter = recommendationFormatter;
    }

    /// <summary>
    /// ハードウェア一覧を表示用の項目にする。
    /// </summary>
    /// <param name="inventory">ハードウェア一覧。</param>
    /// <returns>表示用の項目。</returns>
    public IReadOnlyList<HardwareRow> BuildHardwareRows(HardwareInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        var rows = new List<HardwareRow>();
        if (inventory.Cpu is { } cpu)
        {
            rows.Add(Row(
                HardwareComponent.Cpu,
                cpu.Name,
                UiText.Format("Hw_Cpu_Detail", cpu.PhysicalCores, cpu.LogicalProcessors, cpu.MaxClockMhz / 1000.0)));
        }

        if (inventory.Memory is { } memory)
        {
            rows.Add(Row(
                HardwareComponent.Memory,
                ByteSizeFormatter.Format(memory.TotalBytes),
                memory.SpeedMhz is { } speed
                    ? UiText.Format("Hw_Memory_ModulesWithSpeed", memory.ModuleCount, speed)
                    : UiText.Format("Hw_Memory_Modules", memory.ModuleCount)));
        }

        rows.AddRange(inventory.Storage.Select(d => Row(
            HardwareComponent.Storage,
            d.Model,
            UiText.Format("Hw_Storage_Detail", ByteSizeFormatter.Format(d.SizeBytes), MediaTypeLabel(d.MediaType), d.BusType, HealthLabel(d.Health)))));

        rows.AddRange(inventory.Gpus.Select(g => Row(
            HardwareComponent.Gpu,
            g.Name,
            g.AdapterRamBytes is { } ram
                ? UiText.Format("Hw_Gpu_WithRam", ByteSizeFormatter.Format(ram), g.DriverVersion)
                : UiText.Format("Hw_Gpu_DriverOnly", g.DriverVersion))));

        rows.AddRange(inventory.Batteries.Select(b => Row(HardwareComponent.Battery, b.Name, BatteryDetail(b))));

        if (rows.Count == 0)
        {
            rows.Add(new HardwareRow(HardwareGlyphs.Unknown, "—", UiText.Get("Hw_None_Title"), UiText.Get("Hw_None_Detail")));
        }

        return rows;
    }

    /// <summary>
    /// おすすめを表示用の項目にする。
    /// </summary>
    /// <param name="recommendations">おすすめの一覧。</param>
    /// <returns>表示用の項目。</returns>
    public IReadOnlyList<RecommendationItem> BuildRecommendations(IEnumerable<HardwareRecommendation> recommendations)
    {
        ArgumentNullException.ThrowIfNull(recommendations);
        return recommendations
            .Select(r => (Recommendation: r, Text: _recommendationFormatter.Format(r)))
            .Select(x => new RecommendationItem(HardwareGlyphs.For(x.Recommendation.Component), x.Text.Title, x.Text.Explanation))
            .ToArray();
    }

    /// <summary>
    /// アプリの使用量を表示用の項目にする。
    /// </summary>
    /// <param name="apps">アプリ単位の使用量。</param>
    /// <returns>表示用の項目。</returns>
    public IReadOnlyList<AppUsageItem> BuildTopApps(IEnumerable<ProcessUsage> apps)
    {
        ArgumentNullException.ThrowIfNull(apps);

        var list = apps.ToArray();
        var largest = list.Length == 0 ? 0UL : list.Max(a => a.WorkingSetBytes);
        return list
            .Select(a => new AppUsageItem(
                a.Name,
                ByteSizeFormatter.Format(a.WorkingSetBytes),
                UiText.Format("Apps_ProcessCount", a.ProcessCount),
                largest == 0 ? 0 : 100.0 * a.WorkingSetBytes / largest))
            .ToArray();
    }

    private static HardwareRow Row(HardwareComponent component, string title, string detail) =>
        new(HardwareGlyphs.For(component), ComponentDisplayNames.Get(component), title, detail);

    private static string MediaTypeLabel(StorageMediaType type) =>
        type switch
        {
            StorageMediaType.Ssd => "SSD",
            StorageMediaType.Hdd => "HDD",
            StorageMediaType.Scm => "SCM",
            _ => UiText.Get("Media_Unknown"),
        };

    private static string HealthLabel(StorageHealth health) =>
        health switch
        {
            StorageHealth.Healthy => UiText.Get("Health_Healthy"),
            StorageHealth.Warning => UiText.Get("Health_Warning"),
            StorageHealth.Unhealthy => UiText.Get("Health_Unhealthy"),
            _ => UiText.Get("Health_Unknown"),
        };

    private static string BatteryDetail(BatteryInfo battery)
    {
        var charge = battery.ChargePercent is { } c
            ? UiText.Format("Hw_Battery_Charge", c)
            : UiText.Get("Hw_Battery_ChargeUnknown");
        return battery.HealthRatio is { } ratio
            ? UiText.Format("Hw_Battery_Health", charge, ratio * 100)
            : charge;
    }
}

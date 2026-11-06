using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Yamori.Core.Formatting;
using Yamori.Core.Hardware;
using Yamori.Core.Processes;
using Yamori.Core.Recommendations;

namespace Yamori.Presentation;

/// <summary>
/// <see cref="Core.Reports.NextPcReport"/> の中身を、画面に出す行の一覧に変換する。
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
    /// ハードウェア一覧を表示用の行にする。
    /// </summary>
    /// <param name="inventory">ハードウェア一覧。</param>
    /// <returns>表示用の行。</returns>
    public IReadOnlyList<HardwareRow> BuildHardwareRows(HardwareInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        var rows = new List<HardwareRow>();
        if (inventory.Cpu is { } cpu)
        {
            rows.Add(new HardwareRow(
                "CPU",
                cpu.Name,
                string.Format(CultureInfo.InvariantCulture, "{0} コア / {1} スレッド・最大 {2:0.0} GHz", cpu.PhysicalCores, cpu.LogicalProcessors, cpu.MaxClockMhz / 1000.0)));
        }

        if (inventory.Memory is { } memory)
        {
            var speed = memory.SpeedMhz is { } s ? $"・{s} MT/s" : string.Empty;
            rows.Add(new HardwareRow("メモリ", ByteSizeFormatter.Format(memory.TotalBytes), $"{memory.ModuleCount} 枚{speed}"));
        }

        rows.AddRange(inventory.Storage.Select(d => new HardwareRow(
            "ストレージ",
            d.Model,
            $"{ByteSizeFormatter.Format(d.SizeBytes)}・{MediaTypeLabel(d.MediaType)}（{d.BusType}）・{HealthLabel(d.Health)}")));

        rows.AddRange(inventory.Gpus.Select(g => new HardwareRow(
            "GPU",
            g.Name,
            g.AdapterRamBytes is { } ram
                ? $"ビデオメモリ {ByteSizeFormatter.Format(ram)}（4GB 以上は正しく表示されないことがあります）・ドライバー {g.DriverVersion}"
                : $"ドライバー {g.DriverVersion}")));

        rows.AddRange(inventory.Batteries.Select(b => new HardwareRow("バッテリー", b.Name, BatteryDetail(b))));

        if (rows.Count == 0)
        {
            rows.Add(new HardwareRow("—", "ハードウェア情報を取得できませんでした", "Windows の管理機能（WMI）が応答していない可能性があります。"));
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
            .Select(_recommendationFormatter.Format)
            .Select(t => new RecommendationItem(t.Title, t.Explanation))
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
        return apps
            .Select(a => new AppUsageItem(a.Name, ByteSizeFormatter.Format(a.WorkingSetBytes), $"{a.ProcessCount} プロセス"))
            .ToArray();
    }

    private static string MediaTypeLabel(StorageMediaType type) =>
        type switch
        {
            StorageMediaType.Ssd => "SSD",
            StorageMediaType.Hdd => "HDD",
            StorageMediaType.Scm => "SCM",
            _ => "種類不明",
        };

    private static string HealthLabel(StorageHealth health) =>
        health switch
        {
            StorageHealth.Healthy => "Windows の判定：正常",
            StorageHealth.Warning => "Windows の判定：注意（バックアップをおすすめします）",
            StorageHealth.Unhealthy => "Windows の判定：異常（早めのバックアップと交換をおすすめします）",
            _ => "健康状態は不明",
        };

    private static string BatteryDetail(BatteryInfo battery)
    {
        var charge = battery.ChargePercent is { } c ? $"残量 {c}%" : "残量不明";
        return battery.HealthRatio is { } ratio
            ? string.Format(CultureInfo.InvariantCulture, "{0}・満充電容量は新品時の {1:0}%（80% を下回ると持ちの悪さを感じやすくなります）", charge, ratio * 100)
            : charge;
    }
}

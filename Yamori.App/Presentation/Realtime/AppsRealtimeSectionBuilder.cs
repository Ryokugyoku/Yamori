using System;
using System.Collections.Generic;
using Yamori.Core.Processes;
using Yamori.Core.Realtime;
using Yamori.Localization;

namespace Yamori.Presentation.Realtime;

/// <summary>
/// アプリのカードの表示内容を作る。CPU・GPU・読み書きのそれぞれで、多く使っているアプリを上位から並べる。
/// </summary>
public sealed class AppsRealtimeSectionBuilder : IRealtimeSectionBuilder
{
    /// <inheritdoc />
    public RealtimeSectionData Build(RealtimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var apps = snapshot.Apps;

        var entries = new[]
        {
            CreateEntry("cpu", "Rt_Apps_ByCpu", apps.TopByCpu, a => RealtimeFormat.Percent(a.CpuPercent), a => a.CpuPercent, _ => null),
            CreateEntry("gpu", "Rt_Apps_ByGpu", apps.TopByGpu, a => RealtimeFormat.Percent(a.GpuPercent), a => a.GpuPercent, _ => null),
            CreateEntry("io", "Rt_Apps_ByIo", apps.TopByIo, a => RealtimeFormat.BytesPerSecond(a.IoBytesPerSecond), _ => null, FormatIoDetail),
        };

        var headline = apps.TopByCpu.Count > 0 ? UiText.Format("Rt_Apps_Headline", apps.TopByCpu[0].Name) : "—";
        return new RealtimeSectionData(
            "apps",
            HardwareGlyphs.Apps,
            UiText.Get("Rt_Section_Apps"),
            headline,
            null,
            UiText.Get("Rt_Apps_Note"),
            entries);
    }

    private static RealtimeEntryData CreateEntry(
        string key,
        string titleKey,
        IReadOnlyList<AppActivity> apps,
        Func<AppActivity, string?> format,
        Func<AppActivity, double?> percent,
        Func<AppActivity, string?> detail)
    {
        var facts = new RealtimeFactList();
        foreach (var app in apps)
        {
            facts.AddLabeled(
                "app:" + app.Name,
                UiText.Format("Rt_Apps_AppLabel", app.Name, app.ProcessCount),
                format(app),
                percent(app),
                detail(app));
        }

        var list = facts.ToList();
        return new RealtimeEntryData(key, UiText.Get(titleKey), list.Count == 0 ? UiText.Get("Rt_Apps_None") : string.Empty, list);
    }

    /// <summary>
    /// 読み書きの内訳（値は合計だけにして、内訳はアプリ名の下に小さく出す。値が長いと名前が削られるため）。
    /// </summary>
    private static string? FormatIoDetail(AppActivity app) =>
        UiText.Format(
            "Rt_Apps_ReadWrite",
            RealtimeFormat.BytesPerSecond(app.ReadBytesPerSecond),
            RealtimeFormat.BytesPerSecond(app.WriteBytesPerSecond));
}

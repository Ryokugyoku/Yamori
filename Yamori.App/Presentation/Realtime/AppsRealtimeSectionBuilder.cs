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
            CreateEntry("cpu", "Rt_Apps_ByCpu", apps.TopByCpu, a => RealtimeFormat.Percent(a.CpuPercent), a => a.CpuPercent),
            CreateEntry("gpu", "Rt_Apps_ByGpu", apps.TopByGpu, a => RealtimeFormat.Percent(a.GpuPercent), a => a.GpuPercent),
            CreateEntry("io", "Rt_Apps_ByIo", apps.TopByIo, FormatIo, _ => null),
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
        Func<AppActivity, double?> percent)
    {
        var facts = new RealtimeFactList();
        foreach (var app in apps)
        {
            facts.AddLabeled(
                "app:" + app.Name,
                UiText.Format("Rt_Apps_AppLabel", app.Name, app.ProcessCount),
                format(app),
                percent(app));
        }

        var list = facts.ToList();
        return new RealtimeEntryData(key, UiText.Get(titleKey), list.Count == 0 ? UiText.Get("Rt_Apps_None") : string.Empty, list);
    }

    private static string? FormatIo(AppActivity app) =>
        UiText.Format(
            "Rt_Apps_ReadWrite",
            RealtimeFormat.BytesPerSecond(app.IoBytesPerSecond),
            RealtimeFormat.BytesPerSecond(app.ReadBytesPerSecond),
            RealtimeFormat.BytesPerSecond(app.WriteBytesPerSecond));
}

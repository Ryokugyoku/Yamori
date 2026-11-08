using Yamori.Core.Notifications;

namespace Yamori.Notifier.Localization;

/// <summary>
/// 異常の通知の文面のひな形を、今の表示言語のリソースから作る。
/// </summary>
internal static class NotifierToastTemplates
{
    /// <summary>
    /// 今の UI カルチャのひな形を作る。
    /// </summary>
    /// <returns>ひな形。</returns>
    public static AnomalyToastTemplates Create() => new(
        ThermalThrottlingTitle: NotifierText.Get("Toast_ThermalThrottlingTitle"),
        LimitThrottlingTitle: NotifierText.Get("Toast_LimitThrottlingTitle"),
        WorseThanUsualTitle: NotifierText.Get("Toast_WorseThanUsualTitle"),
        IdleLoadWorseThanUsualTitle: NotifierText.Get("Toast_IdleLoadWorseThanUsualTitle"),
        CpuName: NotifierText.Get("Toast_PartCpu"),
        GpuName: NotifierText.Get("Toast_PartGpu"),
        OtherPartName: NotifierText.Get("Toast_PartOther"),
        Temperature: NotifierText.Get("Toast_Temperature"),
        ClockDrop: NotifierText.Get("Toast_ClockDrop"),
        DeltaFromUsual: NotifierText.Get("Toast_DeltaFromUsual"),
        IdleLoadDeltaFromUsual: NotifierText.Get("Toast_IdleLoadDeltaFromUsual"),
        DurationSeconds: NotifierText.Get("Toast_DurationSeconds"),
        DurationMinutes: NotifierText.Get("Toast_DurationMinutes"),
        DurationHours: NotifierText.Get("Toast_DurationHours"),
        Separator: NotifierText.Get("Toast_Separator"));
}

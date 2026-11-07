using Yamori.Core.Hardware;

namespace Yamori.Core.Realtime.Sources;

/// <summary>
/// 今の電源の状態。
/// </summary>
/// <param name="Source">今使っている電源（AC 電源かバッテリーか）。</param>
/// <param name="HasBattery">バッテリーがあるかどうか。</param>
/// <param name="BatteryPercent">バッテリーの残量（%）。分からない場合は null。</param>
/// <param name="BatteryRemaining">バッテリーで動ける残り時間。充電中や分からない場合は null。</param>
/// <param name="IsBatterySaverOn">バッテリー節約機能が働いているかどうか。</param>
public sealed record PowerStatus(
    PowerSource Source,
    bool HasBattery,
    int? BatteryPercent,
    TimeSpan? BatteryRemaining,
    bool IsBatterySaverOn);

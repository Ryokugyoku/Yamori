using Yamori.Core.Notifications;

namespace Yamori.Notifier.Toasts;

/// <summary>
/// 今、通知を出してよいかを決める。一時停止中と、全画面のアプリを使っている間は出さない
/// （そのとき起きた異常は Helper の一覧に残るので、見落とすことはない）。
/// 夜間などの「応答不可」は Windows 自身が通知を止めるため、ここでは扱わない。
/// </summary>
/// <param name="pause">一時停止の状態。</param>
/// <param name="isUserBusy">利用者が全画面のアプリなどを使っているかを調べる処理。</param>
/// <param name="now">今の時刻を返す処理。</param>
internal sealed class NotificationGate(NotificationPause pause, Func<bool> isUserBusy, Func<DateTimeOffset> now)
{
    private readonly NotificationPause _pause = pause ?? throw new ArgumentNullException(nameof(pause));
    private readonly Func<bool> _isUserBusy = isUserBusy ?? throw new ArgumentNullException(nameof(isUserBusy));
    private readonly Func<DateTimeOffset> _now = now ?? throw new ArgumentNullException(nameof(now));

    /// <summary>
    /// 今、通知を出してよいかどうかを調べる。
    /// </summary>
    /// <returns>出してよいなら true。</returns>
    public bool CanShow() => !_pause.IsPaused(_now()) && !_isUserBusy();
}

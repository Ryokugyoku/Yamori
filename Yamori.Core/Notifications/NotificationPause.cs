namespace Yamori.Core.Notifications;

/// <summary>
/// 通知の一時停止の状態。トレイのメニューの「1 時間」「明日まで」で止め、時刻を過ぎたら自動で戻る。
/// </summary>
public sealed class NotificationPause
{
    private readonly object _gate = new();
    private DateTimeOffset? _until;

    /// <summary>
    /// 一時停止が終わる時刻を取得する。止めていなければ null。
    /// </summary>
    public DateTimeOffset? Until
    {
        get
        {
            lock (_gate)
            {
                return _until;
            }
        }
    }

    /// <summary>
    /// 今から指定した時間だけ通知を止める。
    /// </summary>
    /// <param name="now">今の時刻。</param>
    /// <param name="duration">止める長さ。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> が 0 以下。</exception>
    public void PauseFor(DateTimeOffset now, TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
        Set(now + duration);
    }

    /// <summary>
    /// 明日の 0 時（<paramref name="now"/> の時差での日付の変わり目）まで通知を止める。
    /// </summary>
    /// <param name="now">今の時刻。この時差で「明日」を決める。</param>
    public void PauseUntilTomorrow(DateTimeOffset now)
    {
        var tomorrow = new DateTimeOffset(now.Date.AddDays(1), now.Offset);
        Set(tomorrow);
    }

    /// <summary>
    /// 一時停止をやめ、すぐに通知を出せるようにする。
    /// </summary>
    public void Resume()
    {
        lock (_gate)
        {
            _until = null;
        }
    }

    /// <summary>
    /// 指定した時刻に通知を止めているかどうかを調べる。終わりの時刻を過ぎていたら、止めていない扱いにする。
    /// </summary>
    /// <param name="now">今の時刻。</param>
    /// <returns>止めているなら true。</returns>
    public bool IsPaused(DateTimeOffset now)
    {
        lock (_gate)
        {
            if (_until is { } until && now < until)
            {
                return true;
            }

            _until = null;
            return false;
        }
    }

    private void Set(DateTimeOffset until)
    {
        lock (_gate)
        {
            _until = until;
        }
    }
}

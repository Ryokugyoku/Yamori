using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Detection;

namespace Yamori.Core.Notifications;

/// <summary>
/// 通知専用の常駐プログラムが、受け取った異常の知らせで通知を出すかどうかを決める（一時停止と全画面中は別の部品が見る）。
/// </summary>
/// <remarks>
/// <para>Helper も同じ設定と 6 時間の間隔で <see cref="AnomalyNotice.ShouldNotify"/> を決めているが、ここでも確かめる。
/// 設定を変えた直後の知らせや、通知済みの記録が Helper に届く前に同じ異常の知らせがもう一度来た場合に、二重に出さないため。</para>
/// <para>スレッドセーフ。</para>
/// </remarks>
public sealed class AnomalyToastFilter
{
    private readonly Lock _lock = new();
    private readonly Dictionary<(AnomalyKind Kind, string HardwareKey), DateTimeOffset> _lastShown = [];
    private readonly HashSet<long> _shownIds = [];

    /// <summary>
    /// 間隔を指定して作る。
    /// </summary>
    /// <param name="cooldown">同じ部品の同じ種類の通知を出す最短の間隔。省略時は <see cref="AnomalyNotificationPolicy.DefaultCooldown"/>（6 時間）。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cooldown"/> が負の値。</exception>
    public AnomalyToastFilter(TimeSpan? cooldown = null)
    {
        Cooldown = cooldown ?? AnomalyNotificationPolicy.DefaultCooldown;
        ArgumentOutOfRangeException.ThrowIfLessThan(Cooldown, TimeSpan.Zero, nameof(cooldown));
    }

    /// <summary>同じ部品の同じ種類の通知を出す最短の間隔を取得する。</summary>
    public TimeSpan Cooldown { get; }

    /// <summary>
    /// 設定を読む前に、知らせが通知の候補かどうかを調べる（候補でなければ、設定を読みにいかない）。
    /// </summary>
    /// <param name="notice">異常の知らせ。</param>
    /// <returns>Helper が通知すべきとした、始まった・重くなった知らせで、「これは正常」でなければ true。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="notice"/> が null。</exception>
    public static bool IsCandidate(AnomalyNotice notice)
    {
        ArgumentNullException.ThrowIfNull(notice);
        return notice.ShouldNotify
            && notice.Change is AnomalyChange.Started or AnomalyChange.Escalated
            && !notice.Anomaly.IsDismissed;
    }

    /// <summary>
    /// 通知を出してよいかどうかを決める。
    /// </summary>
    /// <param name="notice">異常の知らせ。</param>
    /// <param name="notificationsEnabled">通知の全体のオン・オフ。分からない（古い Helper など）なら null で、オンとみなす。</param>
    /// <param name="kindSetting">異常の種類の設定。分からなければ null で、オンとみなす。</param>
    /// <param name="nowUtc">今の時刻。</param>
    /// <returns>候補で、設定がオフでなく、この異常をまだ出しておらず、同じ部品の同じ種類を間隔の中で出していなければ true。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="notice"/> が null。</exception>
    public bool Allows(AnomalyNotice notice, bool? notificationsEnabled, AnomalySetting? kindSetting, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(notice);
        if (!IsCandidate(notice) || notificationsEnabled is false || kindSetting is { NotificationsEnabled: false })
        {
            return false;
        }

        var anomaly = notice.Anomaly;
        lock (_lock)
        {
            return !_shownIds.Contains(anomaly.AnomalyId)
                && (!_lastShown.TryGetValue((anomaly.Kind, anomaly.HardwareKey), out var last) || nowUtc - last >= Cooldown);
        }
    }

    /// <summary>
    /// 通知を出したことを覚える。
    /// </summary>
    /// <param name="anomaly">通知を出した異常。</param>
    /// <param name="nowUtc">出した時刻。</param>
    /// <exception cref="ArgumentNullException"><paramref name="anomaly"/> が null。</exception>
    public void RecordShown(AnomalyRecord anomaly, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(anomaly);
        lock (_lock)
        {
            _shownIds.Add(anomaly.AnomalyId);
            _lastShown[(anomaly.Kind, anomaly.HardwareKey)] = nowUtc;
        }
    }
}

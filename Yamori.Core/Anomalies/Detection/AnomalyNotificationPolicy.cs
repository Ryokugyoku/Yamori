namespace Yamori.Core.Anomalies.Detection;

/// <summary>
/// 異常の通知を出すべきかどうかを決める。同じ部品の同じ種類の通知は <see cref="Cooldown"/> に 1 回まで。
/// </summary>
/// <remarks>全画面中や一時停止中かどうかは、通知を出す側（通知専用の常駐プログラム）が判断する。</remarks>
public sealed class AnomalyNotificationPolicy
{
    /// <summary>同じ部品の同じ種類の通知を出す最短の間隔の既定値（6 時間）。</summary>
    public static readonly TimeSpan DefaultCooldown = TimeSpan.FromHours(6);

    /// <summary>
    /// 間隔を指定して作る。
    /// </summary>
    /// <param name="cooldown">同じ部品の同じ種類の通知を出す最短の間隔。省略時は <see cref="DefaultCooldown"/>。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cooldown"/> が負の値。</exception>
    public AnomalyNotificationPolicy(TimeSpan? cooldown = null)
    {
        Cooldown = cooldown ?? DefaultCooldown;
        ArgumentOutOfRangeException.ThrowIfLessThan(Cooldown, TimeSpan.Zero, nameof(cooldown));
    }

    /// <summary>同じ部品の同じ種類の通知を出す最短の間隔を取得する。</summary>
    public TimeSpan Cooldown { get; }

    /// <summary>
    /// 通知を出すべきかどうかを決める。
    /// </summary>
    /// <param name="setting">異常の種類の設定。</param>
    /// <param name="anomaly">異常。</param>
    /// <param name="lastNotifiedUtc">同じ部品の同じ種類で最後に通知を出した時刻。出していなければ null。</param>
    /// <param name="nowUtc">今の時刻。</param>
    /// <returns>通知の設定がオンで、「これは正常」でなく、まだ通知しておらず、最後の通知から間隔が空いていれば true。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> か <paramref name="anomaly"/> が null。</exception>
    public bool ShouldNotify(AnomalySetting setting, AnomalyRecord anomaly, DateTimeOffset? lastNotifiedUtc, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(setting);
        ArgumentNullException.ThrowIfNull(anomaly);

        return setting.NotificationsEnabled
            && !anomaly.IsDismissed
            && anomaly.NotifiedUtc is null
            && (lastNotifiedUtc is not { } last || nowUtc - last >= Cooldown);
    }
}

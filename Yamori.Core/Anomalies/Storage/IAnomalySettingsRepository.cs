namespace Yamori.Core.Anomalies.Storage;

/// <summary>
/// マスタの <c>anomaly_setting</c>（異常の種類ごとの設定）と <c>anomaly_notification</c>（通知の全体のオン・オフ）を読み書きする。
/// 実装はスレッドセーフであること。
/// </summary>
public interface IAnomalySettingsRepository
{
    /// <summary>
    /// 異常の通知の全体のオン・オフを取得する。保存していなければオン。
    /// </summary>
    /// <returns>通知を出すなら true。</returns>
    bool GetNotificationsEnabled();

    /// <summary>
    /// 異常の通知の全体のオン・オフを保存する。
    /// </summary>
    /// <param name="enabled">通知を出すなら true。</param>
    void SaveNotificationsEnabled(bool enabled);

    /// <summary>
    /// すべての種類の設定を取得する。保存していない種類は初期の設定（<see cref="AnomalySettingRules.Default"/>）にする。
    /// </summary>
    /// <returns>種類の順の設定。</returns>
    IReadOnlyList<AnomalySetting> GetAll();

    /// <summary>
    /// 設定を保存する（同じ種類の設定は上書きする）。
    /// </summary>
    /// <param name="setting">設定。</param>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> が null。</exception>
    void Save(AnomalySetting setting);
}

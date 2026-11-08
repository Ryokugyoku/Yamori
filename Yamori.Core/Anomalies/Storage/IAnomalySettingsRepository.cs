namespace Yamori.Core.Anomalies.Storage;

/// <summary>
/// マスタの <c>anomaly_setting</c>（異常の種類ごとの設定）を読み書きする。実装はスレッドセーフであること。
/// </summary>
public interface IAnomalySettingsRepository
{
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

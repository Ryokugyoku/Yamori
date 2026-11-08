using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Anomalies.Service;

/// <summary>
/// 異常の一覧と設定の窓口。Helper の中では <see cref="LocalAnomalyService"/> が使い、GUI と通知専用の常駐プログラムは
/// パイプ（<see cref="Sensors.Ipc.Client.RemoteSensorService"/>）でこれを使う。
/// </summary>
/// <remarks>操作はすべて、管理者でない利用者も使える（安全に関わらないため）。</remarks>
public interface IAnomalyService
{
    /// <summary>
    /// 異常が始まった・重くなった・終わった、または「これは正常」の印が変わったときに発生する。
    /// </summary>
    event EventHandler<AnomalyNotice>? AnomalyChanged;

    /// <summary>
    /// 期間に重なる異常を、新しく始まった順に取得する。
    /// </summary>
    /// <param name="fromUtc">期間の始まり（含む）。</param>
    /// <param name="toUtc">期間の終わり（含まない）。</param>
    /// <param name="includeDismissed">「これは正常」とした異常も含めるなら true。</param>
    /// <param name="limit">取得する最大の件数。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>異常。記録が使えない、または Helper につながっていない場合は空。</returns>
    Task<IReadOnlyList<AnomalyRecord>> GetAnomaliesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        bool includeDismissed,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 「これは正常」の印を付ける、または外す。
    /// </summary>
    /// <param name="anomalyId">異常の番号。</param>
    /// <param name="dismissed">付けるなら true、外すなら false。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>結果。異常がなければ <see cref="SensorSettingResult.NotFound"/>。</returns>
    Task<SensorSettingResult> SetDismissedAsync(long anomalyId, bool dismissed, CancellationToken cancellationToken = default);

    /// <summary>
    /// 通知を出したことを知らせる（同じ部品の同じ種類の通知を 6 時間に 1 回までにするため）。
    /// 全画面中や一時停止中で出さなかった場合は呼ばない。
    /// </summary>
    /// <param name="anomalyId">異常の番号。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>結果。異常がなければ <see cref="SensorSettingResult.NotFound"/>。</returns>
    Task<SensorSettingResult> MarkNotifiedAsync(long anomalyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// すべての種類の設定を取得する。
    /// </summary>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>種類の順の設定。記録が使えない、または Helper につながっていない場合は空。</returns>
    Task<IReadOnlyList<AnomalySetting>> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 種類の設定を変える。
    /// </summary>
    /// <param name="setting">新しい設定。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>結果。値が範囲の外なら <see cref="SensorSettingResult.Invalid"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> が null。</exception>
    Task<SensorSettingResult> SetSettingAsync(AnomalySetting setting, CancellationToken cancellationToken = default);
}

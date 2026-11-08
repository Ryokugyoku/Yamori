using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Service;

namespace Yamori.Core.Notifications;

/// <summary>
/// 通知専用の常駐プログラムで、Helper から届いた異常の知らせを通知にする流れをまとめる。
/// 候補か確かめ → Helper から最新の設定を読み → 出してよいか決め → 出し → 出したら Helper に通知済みを記録する。
/// </summary>
/// <remarks>知らせは 1 つずつ順に処理する（同じ異常の知らせが続けて届いても、二重に出さないため）。</remarks>
public sealed class AnomalyNotificationDispatcher
{
    private readonly IAnomalyService _anomalies;
    private readonly AnomalyToastFilter _filter;
    private readonly IAnomalyToastSink _sink;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _serial = new(1, 1);

    /// <summary>
    /// 部品を指定して作る。
    /// </summary>
    /// <param name="anomalies">異常の設定と通知済みの記録の窓口（Helper へのパイプ）。</param>
    /// <param name="filter">通知を出してよいかの決め方。</param>
    /// <param name="sink">通知を出す先。</param>
    /// <param name="timeProvider">現在時刻の提供元。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public AnomalyNotificationDispatcher(IAnomalyService anomalies, AnomalyToastFilter filter, IAnomalyToastSink sink, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(anomalies);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _anomalies = anomalies;
        _filter = filter;
        _sink = sink;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// 異常の知らせを処理する。
    /// </summary>
    /// <param name="notice">異常の知らせ。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>結果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="notice"/> が null。</exception>
    public async Task<AnomalyNotificationOutcome> HandleAsync(AnomalyNotice notice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notice);
        if (!AnomalyToastFilter.IsCandidate(notice))
        {
            return AnomalyNotificationOutcome.NotCandidate;
        }

        await _serial.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // 設定は知らせのたびに読み直す（画面で変えた設定にすぐ従うため。知らせはまれなので負担にならない）。
            var enabled = await _anomalies.GetNotificationsEnabledAsync(cancellationToken).ConfigureAwait(false);
            var settings = await _anomalies.GetSettingsAsync(cancellationToken).ConfigureAwait(false);
            var kindSetting = settings.FirstOrDefault(s => s.Kind == notice.Anomaly.Kind);
            if (!_filter.Allows(notice, enabled, kindSetting, _timeProvider.GetUtcNow()))
            {
                return AnomalyNotificationOutcome.Suppressed;
            }

            if (!await _sink.TryShowAsync(notice.Anomaly).ConfigureAwait(false))
            {
                // 一時停止中・全画面中。異常は Helper の一覧に残っている。通知済みにはしない（6 時間の数え始めにしない）。
                return AnomalyNotificationOutcome.Held;
            }

            _filter.RecordShown(notice.Anomaly, _timeProvider.GetUtcNow());
            await _anomalies.MarkNotifiedAsync(notice.Anomaly.AnomalyId, cancellationToken).ConfigureAwait(false);
            return AnomalyNotificationOutcome.Shown;
        }
        finally
        {
            _serial.Release();
        }
    }
}

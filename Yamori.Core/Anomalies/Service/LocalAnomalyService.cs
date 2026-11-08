using Yamori.Core.Anomalies.Detection;
using Yamori.Core.Anomalies.Storage;
using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Anomalies.Service;

/// <summary>
/// 同じプロセスの中の異常の一覧と設定を扱う <see cref="IAnomalyService"/>。Helper の中で使う。
/// </summary>
/// <remarks>マスタを開けなかった場合は、一覧と設定は空、変更は <see cref="SensorSettingResult.Unavailable"/> を返す。</remarks>
public sealed class LocalAnomalyService : IAnomalyService
{
    /// <summary>1 回に返す異常の数の上限。</summary>
    public const int MaxLimit = 1000;

    private readonly AnomalyDetector? _detector;
    private readonly IAnomalyRepository? _repository;
    private readonly AnomalySettingsCache? _settings;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// 部品を指定して作る。
    /// </summary>
    /// <param name="detector">検出の仕組み。知らせはここから流す。記録が使えない場合は null。</param>
    /// <param name="repository">異常の一覧の保存先。記録が使えない場合は null。</param>
    /// <param name="settings">異常の設定。記録が使えない場合は null。</param>
    /// <param name="timeProvider">現在時刻の提供元。</param>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> が null。</exception>
    public LocalAnomalyService(AnomalyDetector? detector, IAnomalyRepository? repository, AnomalySettingsCache? settings, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _detector = detector;
        _repository = repository;
        _settings = settings;
        _timeProvider = timeProvider;
        if (_detector is not null)
        {
            _detector.AnomalyChanged += (_, notice) => AnomalyChanged?.Invoke(this, notice);
        }
    }

    /// <inheritdoc />
    public event EventHandler<AnomalyNotice>? AnomalyChanged;

    /// <summary>異常の一覧と設定が使えるかどうかを取得する。</summary>
    public bool IsAvailable => _repository is not null && _settings is not null;

    /// <inheritdoc />
    public Task<IReadOnlyList<AnomalyRecord>> GetAnomaliesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        bool includeDismissed,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(limit);
        if (_repository is not { } repository || toUtc <= fromUtc || limit == 0)
        {
            return Task.FromResult<IReadOnlyList<AnomalyRecord>>([]);
        }

        return Task.Run(() => repository.GetOverlapping(fromUtc, toUtc, includeDismissed, Math.Min(limit, MaxLimit)), cancellationToken);
    }

    /// <inheritdoc />
    public Task<SensorSettingResult> SetDismissedAsync(long anomalyId, bool dismissed, CancellationToken cancellationToken = default)
    {
        if (_repository is not { } repository)
        {
            return Task.FromResult(SensorSettingResult.Unavailable);
        }

        return Task.Run(
            () =>
            {
                if (repository.SetDismissed(anomalyId, dismissed ? _timeProvider.GetUtcNow() : null) is not { } changed)
                {
                    return SensorSettingResult.NotFound;
                }

                _detector?.OnDismissalChanged(changed);
                AnomalyChanged?.Invoke(this, new AnomalyNotice(AnomalyChange.DismissalChanged, changed, ShouldNotify: false));
                return SensorSettingResult.Applied;
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<SensorSettingResult> MarkNotifiedAsync(long anomalyId, CancellationToken cancellationToken = default)
    {
        if (_repository is not { } repository)
        {
            return Task.FromResult(SensorSettingResult.Unavailable);
        }

        return Task.Run(
            () =>
            {
                var now = _timeProvider.GetUtcNow();
                if (repository.Find(anomalyId) is not { } anomaly || !repository.MarkNotified(anomalyId, now))
                {
                    return SensorSettingResult.NotFound;
                }

                _detector?.OnNotified(anomaly, now);
                return SensorSettingResult.Applied;
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AnomalySetting>> GetSettingsAsync(CancellationToken cancellationToken = default) =>
        _settings is { } settings
            ? Task.Run(settings.GetAll, cancellationToken)
            : Task.FromResult<IReadOnlyList<AnomalySetting>>([]);

    /// <inheritdoc />
    public Task<SensorSettingResult> SetSettingAsync(AnomalySetting setting, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(setting);
        if (!AnomalySettingRules.IsValid(setting))
        {
            return Task.FromResult(SensorSettingResult.Invalid);
        }

        if (_settings is not { } settings)
        {
            return Task.FromResult(SensorSettingResult.Unavailable);
        }

        return Task.Run(
            () =>
            {
                settings.Save(setting);
                return SensorSettingResult.Applied;
            },
            cancellationToken);
    }
}

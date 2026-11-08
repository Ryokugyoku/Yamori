using Yamori.Core.Sensors;

namespace Yamori.Core.Anomalies.Detection.Throttling;

/// <summary>
/// 部品 1 つのスロットリングの続き具合を毎秒数え、続いた時間の下限（<see cref="ThrottlingCriteria"/>）を満たしたら異常の判断を返す。
/// </summary>
/// <remarks>
/// <para>熱の制限の印・注意の温度以上・電力の制限・電流の制限のどれかが出ている間を 1 つの続きとして数える。判断する前は
/// <see cref="ThrottlingCriteria.GapTolerance"/>、判断したあとは <see cref="ThrottlingCriteria.ReleaseDelay"/> だけ途切れても続きとみなす。</para>
/// <para>スレッドセーフではない（<see cref="AnomalyDetector"/> から 1 つずつ呼ばれる）。</para>
/// </remarks>
public sealed class ThrottlingEpisodeTracker
{
    private readonly string _hardwareKey;
    private readonly HardwareKind _hardwareKind;
    private readonly ClockBaseline _baseline = new();
    private DateTimeOffset? _lastTick;
    private DateTimeOffset? _since;
    private DateTimeOffset _lastSignal;
    private bool _reported;
    private bool _critical;
    private double _thermalSeconds;
    private double _aboveThresholdSeconds;
    private double _powerSeconds;
    private double _currentSeconds;
    private double _peakTemperature = double.NaN;
    private double _thresholdCelsius;
    private double? _tjMaxCelsius;
    private double _loadSum;
    private int _loadCount;
    private double _clockSum;
    private int _clockCount;
    private double? _baselineClock;

    /// <summary>
    /// 部品を指定して作る。
    /// </summary>
    /// <param name="hardwareKey">部品のキー。</param>
    /// <param name="hardwareKind">部品の種類。</param>
    /// <exception cref="ArgumentNullException"><paramref name="hardwareKey"/> が null。</exception>
    public ThrottlingEpisodeTracker(string hardwareKey, HardwareKind hardwareKind)
    {
        ArgumentNullException.ThrowIfNull(hardwareKey);
        _hardwareKey = hardwareKey;
        _hardwareKind = hardwareKind;
    }

    /// <summary>最後に部品の値を読めた時刻を取得または設定する（読めない部品を忘れるために、規則が使う）。</summary>
    public DateTimeOffset LastObservedUtc { get; set; }

    /// <summary>
    /// 1 回分の材料で数え直す。
    /// </summary>
    /// <param name="nowUtc">判定する時刻。</param>
    /// <param name="signals">今の材料。</param>
    /// <param name="thresholdCelsius">注意の温度（℃）。</param>
    /// <returns>異常と判断している間は、その判断。そうでなければ null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="signals"/> が null。</exception>
    public AnomalyFinding? Update(DateTimeOffset nowUtc, ThrottlingSignals signals, double thresholdCelsius)
    {
        ArgumentNullException.ThrowIfNull(signals);

        var tick = _lastTick is { } last
            ? Math.Clamp((nowUtc - last).TotalSeconds, 0, ThrottlingCriteria.MaxTickDuration.TotalSeconds)
            : 1;
        _lastTick = nowUtc;

        var thermal = signals.ThermalLimit || signals.CriticalTemperature;
        var hot = signals.TemperatureCelsius >= thresholdCelsius;
        var limited = signals.PowerLimit || signals.CurrentLimit;
        if (!thermal && !hot && !limited)
        {
            return Idle(nowUtc, signals);
        }

        if (_since is null)
        {
            Begin(nowUtc);
        }

        _lastSignal = nowUtc;
        _thresholdCelsius = thresholdCelsius;
        _tjMaxCelsius = signals.TjMaxCelsius ?? _tjMaxCelsius;
        _critical |= signals.CriticalTemperature;
        _thermalSeconds += thermal ? tick : 0;
        _aboveThresholdSeconds += hot ? tick : 0;
        _powerSeconds += signals.PowerLimit ? tick : 0;
        _currentSeconds += signals.CurrentLimit ? tick : 0;
        Accumulate(signals);

        _reported |= _critical
            || _thermalSeconds >= ThrottlingCriteria.ThermalLimitMinimum.TotalSeconds
            || _aboveThresholdSeconds >= ThrottlingCriteria.AboveThresholdMinimum.TotalSeconds
            || _powerSeconds >= ThrottlingCriteria.PowerOrCurrentLimitMinimum.TotalSeconds
            || _currentSeconds >= ThrottlingCriteria.PowerOrCurrentLimitMinimum.TotalSeconds;
        return _reported ? Finding(nowUtc) : null;
    }

    private AnomalyFinding? Idle(DateTimeOffset nowUtc, ThrottlingSignals signals)
    {
        if (_since is not null)
        {
            var allowed = _reported ? ThrottlingCriteria.ReleaseDelay : ThrottlingCriteria.GapTolerance;
            if (nowUtc - _lastSignal <= allowed)
            {
                // 途切れてもまだ続きとみなす（終わりかけの出入りで細切れにしない）。
                return _reported ? Finding(nowUtc) : null;
            }

            Reset();
        }

        if (signals.ClockHertz is { } clock)
        {
            _baseline.Add(nowUtc, clock);
        }

        return null;
    }

    private void Begin(DateTimeOffset nowUtc)
    {
        _since = nowUtc;
        _baselineClock = _baseline.Max(nowUtc);
    }

    private void Accumulate(ThrottlingSignals signals)
    {
        if (signals.TemperatureCelsius is { } temperature && (double.IsNaN(_peakTemperature) || temperature > _peakTemperature))
        {
            _peakTemperature = temperature;
        }

        if (signals.LoadPercent is { } load)
        {
            _loadSum += load;
            _loadCount++;
        }

        if (signals.ClockHertz is { } clock)
        {
            _clockSum += clock;
            _clockCount++;
        }
    }

    private void Reset()
    {
        _since = null;
        _reported = false;
        _critical = false;
        _thermalSeconds = 0;
        _aboveThresholdSeconds = 0;
        _powerSeconds = 0;
        _currentSeconds = 0;
        _peakTemperature = double.NaN;
        _tjMaxCelsius = null;
        _loadSum = 0;
        _loadCount = 0;
        _clockSum = 0;
        _clockCount = 0;
        _baselineClock = null;
    }

    private AnomalyFinding Finding(DateTimeOffset nowUtc)
    {
        var evidence = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            [AnomalyEvidenceNames.ThresholdCelsius] = _thresholdCelsius,
            [AnomalyEvidenceNames.DurationSeconds] = Math.Round((nowUtc - _since!.Value).TotalSeconds),
            [AnomalyEvidenceNames.ThermalLimitSeconds] = Math.Round(_thermalSeconds),
            [AnomalyEvidenceNames.AboveThresholdSeconds] = Math.Round(_aboveThresholdSeconds),
            [AnomalyEvidenceNames.PowerLimitSeconds] = Math.Round(_powerSeconds),
            [AnomalyEvidenceNames.CurrentLimitSeconds] = Math.Round(_currentSeconds),
        };

        if (!double.IsNaN(_peakTemperature))
        {
            evidence[AnomalyEvidenceNames.PeakTemperatureCelsius] = _peakTemperature;
        }

        if (_tjMaxCelsius is { } tjMax)
        {
            evidence[AnomalyEvidenceNames.TjMaxCelsius] = tjMax;
        }

        if (_loadCount > 0)
        {
            evidence[AnomalyEvidenceNames.LoadPercent] = Math.Round(_loadSum / _loadCount, 1);
        }

        double? clockDrop = null;
        if (_clockCount > 0)
        {
            var clock = _clockSum / _clockCount;
            evidence[AnomalyEvidenceNames.ClockHertz] = Math.Round(clock);
            if (_baselineClock is { } baseline && baseline > 0)
            {
                clockDrop = Math.Round(Math.Max(0, (baseline - clock) / baseline * 100), 1);
                evidence[AnomalyEvidenceNames.BaselineClockHertz] = Math.Round(baseline);
                evidence[AnomalyEvidenceNames.ClockDropPercent] = clockDrop.Value;
            }
        }

        return new AnomalyFinding(_hardwareKey, _hardwareKind, Severity(clockDrop), evidence, _since, IsNotifiable(clockDrop));
    }

    /// <summary>
    /// 通知の対象にするかどうか。電力・電流の制限だけで、クロックも下がっていなければ、高負荷のふつうの動きとみなして一覧にだけ残す。
    /// </summary>
    private bool IsNotifiable(double? clockDropPercent) =>
        _critical
        || _thermalSeconds >= ThrottlingCriteria.ThermalLimitMinimum.TotalSeconds
        || _aboveThresholdSeconds >= ThrottlingCriteria.AboveThresholdMinimum.TotalSeconds
        || clockDropPercent >= ThrottlingCriteria.NotifyClockDropPercent;

    private AnomalySeverity Severity(double? clockDropPercent)
    {
        if (_critical || _thermalSeconds >= ThrottlingCriteria.CriticalThermalDuration.TotalSeconds)
        {
            return AnomalySeverity.Critical;
        }

        var warningSeconds = ThrottlingCriteria.WarningThermalDuration.TotalSeconds;
        return _thermalSeconds >= warningSeconds
            || _aboveThresholdSeconds >= warningSeconds
            || clockDropPercent >= ThrottlingCriteria.WarningClockDropPercent
                ? AnomalySeverity.Warning
                : AnomalySeverity.Notice;
    }
}

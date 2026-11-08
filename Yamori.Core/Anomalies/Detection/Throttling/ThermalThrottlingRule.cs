using Yamori.Core.Anomalies.Thresholds;

namespace Yamori.Core.Anomalies.Detection.Throttling;

/// <summary>
/// サーマルスロットリング（熱・電力・電流で性能を落としている）の規則。CPU・GPU・温度ゾーンごとに、
/// 制限の印と、注意の温度（<see cref="TemperatureThresholds"/>）以上の時間を数え、続いた時間の下限を満たしたら異常にする。
/// </summary>
/// <remarks>
/// <para>注意の温度は、手動の指定があればそれを（すべての部品に使う）、なければ部品が報告する性能を落とし始める温度
/// （CPU は熱制御の作動温度、GPU はドライバーの温度の上限）の 5℃ 下を、分からなければ部品の種類ごとの初期値を使う。</para>
/// <para>電力・電流の制限も異常に含めるが、高負荷のときのふつうの動きでも短く出るため、熱の制限より長く続いたときだけにする（開発者の判断）。</para>
/// </remarks>
public sealed class ThermalThrottlingRule : IAnomalyRule
{
    /// <summary>値が読めないままこれだけたった部品は忘れる。</summary>
    public static readonly TimeSpan ForgetAfter = TimeSpan.FromMinutes(10);

    private static readonly ThrottlingSignals NoSignals = new(null, null, null, false, false, false, false, null, null);

    private readonly IReadOnlyList<IThrottlingSignalSource> _sources;
    private readonly Dictionary<string, ThrottlingEpisodeTracker> _trackers = new(StringComparer.Ordinal);

    /// <summary>
    /// 材料の読み方を指定して作る。
    /// </summary>
    /// <param name="sources">部品の種類ごとの材料の読み方。省略時は CPU・GPU・温度ゾーン。</param>
    public ThermalThrottlingRule(IEnumerable<IThrottlingSignalSource>? sources = null)
    {
        _sources = sources?.ToArray()
            ?? [new CpuThrottlingSignalSource(), new GpuThrottlingSignalSource(), new ThermalZoneThrottlingSignalSource()];
    }

    /// <inheritdoc />
    public AnomalyKind Kind => AnomalyKind.ThermalThrottling;

    /// <inheritdoc />
    public IReadOnlyList<AnomalyFinding> Evaluate(AnomalyFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var findings = new List<AnomalyFinding>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var observation in _sources.SelectMany(s => s.Read(frame)))
        {
            if (!seen.Add(observation.HardwareKey))
            {
                continue;
            }

            if (!_trackers.TryGetValue(observation.HardwareKey, out var tracker))
            {
                tracker = new ThrottlingEpisodeTracker(observation.HardwareKey, observation.HardwareKind);
                _trackers[observation.HardwareKey] = tracker;
            }

            var threshold = TemperatureThresholds.Resolve(
                observation.HardwareKind,
                observation.Signals.ReportedLimitCelsius,
                frame.Setting.ManualThreshold);
            tracker.LastObservedUtc = frame.TimestampUtc;
            if (tracker.Update(frame.TimestampUtc, observation.Signals, threshold) is { } finding)
            {
                findings.Add(finding);
            }
        }

        // 今回値が読めなかった部品は「材料なし」として数える（一瞬読めないだけで異常を終わらせないため。終わるのは ReleaseDelay のあと）。
        // しばらく読めないままの部品（取り外した GPU など）は忘れる。
        foreach (var (key, tracker) in _trackers.Where(t => !seen.Contains(t.Key)).ToArray())
        {
            if (tracker.Update(frame.TimestampUtc, NoSignals, double.PositiveInfinity) is { } finding)
            {
                findings.Add(finding);
            }
            else if (frame.TimestampUtc - tracker.LastObservedUtc > ForgetAfter)
            {
                _trackers.Remove(key);
            }
        }

        return findings;
    }
}

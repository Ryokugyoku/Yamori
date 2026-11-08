namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// 「ふだんより悪い」の規則。待機中の温度（ほこりやファンの劣化の兆し）と待機中の負荷（裏で動くアプリの兆し）を、
/// 直近 14 日の同じくらいの負荷の時間と比べ、高い状態が続いたら異常にする。機械学習は使わない。
/// </summary>
/// <remarks>
/// <para>ほかの規則と同じく <see cref="AnomalyDetector"/> から毎秒呼ばれるが、判定は 1 分集計を読んで
/// <see cref="UsualComparisonOptions.EvaluationInterval"/> ごと（1 時間ごと）にだけ、収集を止めないよう裏で行う。
/// その間は前回の判断をそのまま返す（同じ異常が続いている間は同じ 1 件になる）。</para>
/// <para>記録が <see cref="UsualComparisonOptions.MinimumHistory"/>（7 日）に満たない間は判定しない。読み込みに失敗したときは前回の判断を残す。</para>
/// </remarks>
public sealed class WorseThanUsualRule : IAnomalyRule
{
    /// <summary>Helper が起動してから最初に判定するまでの待ち時間（センサーの一覧がそろうのを待つ）。</summary>
    public static readonly TimeSpan FirstEvaluationDelay = TimeSpan.FromMinutes(2);

    private readonly IUsualHistorySource _source;
    private readonly UsualComparisonOptions _options;
    private readonly Func<Action, Task> _run;
    private DateTimeOffset? _nextEvaluationUtc;
    private Task? _running;
    private volatile IReadOnlyList<AnomalyFinding> _findings = [];

    /// <summary>
    /// 記録の読み方を指定して作る。
    /// </summary>
    /// <param name="source">1 分ごとの記録の読み方。</param>
    /// <param name="options">決まりごと。省略時は <see cref="UsualComparisonOptions.Default"/>。</param>
    /// <param name="run">判定を裏で動かす方法。省略時はスレッド プール（テストではその場で動かす）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> が null。</exception>
    public WorseThanUsualRule(IUsualHistorySource source, UsualComparisonOptions? options = null, Func<Action, Task>? run = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
        _options = options ?? UsualComparisonOptions.Default;
        _run = run ?? (action => Task.Run(action));
    }

    /// <inheritdoc />
    public AnomalyKind Kind => AnomalyKind.WorseThanUsual;

    /// <summary>最後に判定した時刻を取得する。まだ判定していなければ null。</summary>
    public DateTimeOffset? LastEvaluatedUtc { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<AnomalyFinding> Evaluate(AnomalyFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var now = frame.TimestampUtc;
        _nextEvaluationUtc ??= now + FirstEvaluationDelay;
        if (now >= _nextEvaluationUtc && _running is not { IsCompleted: false })
        {
            _nextEvaluationUtc = UsualStatistics.FloorTo(now, _options.EvaluationInterval) + _options.EvaluationInterval + _options.EvaluationDelay;
            var parts = UsualPartDefinitions.From(frame.Keys);
            var threshold = frame.Setting.ManualThreshold ?? _options.TemperatureDeltaCelsius;
            LastEvaluatedUtc = now;
            _running = _run(() => Compute(parts, now, threshold));
        }

        return _findings;
    }

    private void Compute(IReadOnlyList<UsualPartDefinition> parts, DateTimeOffset now, double threshold)
    {
        try
        {
            var (from, to) = UsualComparisonAnalyzer.RangeFor(now, _options);
            var findings = new List<AnomalyFinding>();
            foreach (var part in parts)
            {
                var minutes = _source.Read(part, from, to);
                var result = UsualComparisonAnalyzer.Analyze(minutes, now, threshold, part.CompareIdleLoad, _options);
                if (UsualFindingFactory.Create(part, result, threshold, _options) is { } finding)
                {
                    findings.Add(finding);
                }
            }

            _findings = findings;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 記録の読み込みの失敗など。前回の判断を残し、次の回にやり直す（一時的な失敗で異常を終わらせないため）。
        }
    }
}

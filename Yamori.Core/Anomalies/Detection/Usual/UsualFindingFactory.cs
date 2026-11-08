namespace Yamori.Core.Anomalies.Detection.Usual;

/// <summary>
/// ふだんと比べた結果から、部品 1 つの判断（<see cref="AnomalyFinding"/>）を作る。待機中の温度と待機中の負荷の両方が高ければ、1 件にまとめる。
/// </summary>
public static class UsualFindingFactory
{
    /// <summary>
    /// 判断を作る。
    /// </summary>
    /// <param name="part">部品。</param>
    /// <param name="result">ふだんと比べた結果。</param>
    /// <param name="temperatureThreshold">温度のしきい値（℃）。</param>
    /// <param name="options">決まりごと。</param>
    /// <returns>判断。どちらも高くなければ null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="temperatureThreshold"/> 以外の引数が null。</exception>
    public static AnomalyFinding? Create(
        UsualPartDefinition part,
        UsualComparisonResult result,
        double temperatureThreshold,
        UsualComparisonOptions options)
    {
        ArgumentNullException.ThrowIfNull(part);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(options);

        if (result.Temperature is null && result.IdleLoad is null)
        {
            return null;
        }

        var evidence = new Dictionary<string, double>(StringComparer.Ordinal);
        var severity = AnomalySeverity.Notice;
        var since = DateTimeOffset.MaxValue;
        var elevated = TimeSpan.Zero;

        if (result.Temperature is { } temperature)
        {
            evidence[AnomalyEvidenceNames.BaselineValue] = Round(temperature.Baseline);
            evidence[AnomalyEvidenceNames.DeltaFromBaseline] = Round(temperature.Delta);
            evidence[AnomalyEvidenceNames.IdleTemperatureCelsius] = Round(temperature.Value);
            severity = Max(severity, SeverityOf(temperature.Delta, temperatureThreshold, options));
            since = Min(since, temperature.StartUtc);
            elevated = Max(elevated, temperature.ElevatedDuration);
        }

        if (result.IdleLoad is { } load)
        {
            evidence[AnomalyEvidenceNames.BaselineIdleLoadPercent] = Round(load.Baseline);
            evidence[AnomalyEvidenceNames.IdleLoadDeltaPoints] = Round(load.Delta);
            evidence[AnomalyEvidenceNames.IdleLoadPercent] = Round(load.Value);
            severity = Max(severity, SeverityOf(load.Delta, options.LoadDeltaPoints, options));
            since = Min(since, load.StartUtc);
            elevated = Max(elevated, load.ElevatedDuration);
        }

        evidence[AnomalyEvidenceNames.DurationSeconds] = elevated.TotalSeconds;
        return new AnomalyFinding(part.HardwareKey, part.HardwareKind, severity, evidence, since);
    }

    private static AnomalySeverity SeverityOf(double delta, double threshold, UsualComparisonOptions options) =>
        delta >= threshold * options.WarningFactor ? AnomalySeverity.Warning : AnomalySeverity.Notice;

    private static double Round(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private static AnomalySeverity Max(AnomalySeverity a, AnomalySeverity b) => a >= b ? a : b;

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a >= b ? a : b;

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a <= b ? a : b;
}

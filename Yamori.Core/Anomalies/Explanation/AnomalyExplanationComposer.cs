using System.Globalization;

namespace Yamori.Core.Anomalies.Explanation;

/// <summary>
/// 異常の判断の理由を一文にする（例：「熱による制限の印が 42 秒、最高温度が 97℃ だったため、「警告」と判断しました。」）。
/// 理由の数値（<see cref="AnomalyRecord.Evidence"/>）のうち、ある項目だけを使うため、異常の種類が増えても同じ型で書ける。
/// </summary>
public sealed class AnomalyExplanationComposer
{
    /// <summary>書くクロックの下がり方の下限（%）。これより小さい下がり方は、ふだんの揺れと区別できないため書かない。</summary>
    public const double MinimumClockDropPercent = 5;

    private readonly AnomalyExplanationTemplates _templates;
    private readonly CultureInfo _culture;

    /// <summary>
    /// ひな形と、数値の書き方の文化を指定して作る。
    /// </summary>
    /// <param name="templates">表示言語のひな形。</param>
    /// <param name="culture">数値の書き方の文化。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public AnomalyExplanationComposer(AnomalyExplanationTemplates templates, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentNullException.ThrowIfNull(culture);
        _templates = templates;
        _culture = culture;
    }

    /// <summary>
    /// 判断の理由の一文を作る。
    /// </summary>
    /// <param name="anomaly">異常。</param>
    /// <returns>一文。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="anomaly"/> が null。</exception>
    public string Compose(AnomalyRecord anomaly)
    {
        ArgumentNullException.ThrowIfNull(anomaly);

        var severity = SeverityName(anomaly.Severity);
        var reasons = Reasons(anomaly).ToArray();
        return reasons.Length == 0
            ? Format(_templates.SentenceWithoutReasons, severity)
            : Format(_templates.Sentence, Join(reasons), severity);
    }

    /// <summary>
    /// 重さの名前を取得する。
    /// </summary>
    /// <param name="severity">重さ。</param>
    /// <returns>名前。</returns>
    public string SeverityName(AnomalySeverity severity) => severity switch
    {
        AnomalySeverity.Critical => _templates.SeverityCritical,
        AnomalySeverity.Warning => _templates.SeverityWarning,
        _ => _templates.SeverityNotice,
    };

    /// <summary>
    /// 長さを、秒・分・時間のうち読みやすい単位で書く。
    /// </summary>
    /// <param name="duration">長さ。</param>
    /// <returns>表示用の文字列。</returns>
    public string FormatDuration(TimeSpan duration)
    {
        var seconds = Math.Max(0, (long)Math.Round(duration.TotalSeconds));
        if (seconds < 60)
        {
            return Format(_templates.DurationSeconds, seconds);
        }

        return seconds < 3600
            ? Format(_templates.DurationMinutes, seconds / 60, seconds % 60)
            : Format(_templates.DurationHours, seconds / 3600, seconds % 3600 / 60);
    }

    private IEnumerable<string> Reasons(AnomalyRecord anomaly)
    {
        if (Positive(anomaly, AnomalyEvidenceNames.ThermalLimitSeconds) is { } thermal)
        {
            yield return Format(_templates.ThermalLimit, Seconds(thermal));
        }

        if (Positive(anomaly, AnomalyEvidenceNames.AboveThresholdSeconds) is { } above
            && AnomalyEvidence.Get(anomaly, AnomalyEvidenceNames.ThresholdCelsius) is { } threshold)
        {
            yield return Format(_templates.AboveThreshold, Seconds(above), Temperature(threshold));
        }

        if (Positive(anomaly, AnomalyEvidenceNames.PowerLimitSeconds) is { } power)
        {
            yield return Format(_templates.PowerLimit, Seconds(power));
        }

        if (Positive(anomaly, AnomalyEvidenceNames.CurrentLimitSeconds) is { } current)
        {
            yield return Format(_templates.CurrentLimit, Seconds(current));
        }

        if (AnomalyEvidence.Get(anomaly, AnomalyEvidenceNames.PeakTemperatureCelsius) is { } peak)
        {
            yield return Format(_templates.PeakTemperature, Temperature(peak));
        }

        if (AnomalyEvidence.Get(anomaly, AnomalyEvidenceNames.ClockDropPercent) is { } drop && drop >= MinimumClockDropPercent)
        {
            yield return Format(_templates.ClockDrop, Round(drop));
        }

        if (Positive(anomaly, AnomalyEvidenceNames.DeltaFromBaseline) is { } delta
            && AnomalyEvidence.Get(anomaly, AnomalyEvidenceNames.BaselineValue) is { } baseline)
        {
            yield return Format(_templates.DeltaFromUsual, Temperature(baseline), Temperature(delta));
        }
    }

    private string Join(IReadOnlyList<string> items) =>
        items.Count == 1
            ? items[0]
            : string.Join(_templates.Separator, items.Take(items.Count - 1)) + _templates.LastSeparator + items[^1];

    private static double? Positive(AnomalyRecord anomaly, string name) =>
        AnomalyEvidence.Get(anomaly, name) is { } value && value > 0 ? value : null;

    private string Seconds(double seconds) => FormatDuration(TimeSpan.FromSeconds(seconds));

    private string Temperature(double celsius) => Format(_templates.Temperature, Round(celsius));

    private string Format(string template, params object[] args) => string.Format(_culture, template, args);

    private static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}

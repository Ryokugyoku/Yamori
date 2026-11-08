using System.Globalization;
using Yamori.Core.Anomalies;
using Yamori.Core.Sensors;

namespace Yamori.Core.Notifications;

/// <summary>
/// 異常から通知の文面（部品・現象・数値）を作る。アプリ名は書かない。
/// </summary>
public sealed class AnomalyToastComposer
{
    /// <summary>本文に出すクロックの下がり方の下限（%）。これより小さい下がり方は書かない（ふだんの揺れと区別できないため）。</summary>
    public const double MinimumClockDropPercent = 5;

    private readonly AnomalyToastTemplates _templates;
    private readonly CultureInfo _culture;

    /// <summary>
    /// ひな形と、数値の書き方の文化を指定して作る。
    /// </summary>
    /// <param name="templates">表示言語のひな形。</param>
    /// <param name="culture">数値の書き方の文化。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public AnomalyToastComposer(AnomalyToastTemplates templates, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentNullException.ThrowIfNull(culture);
        _templates = templates;
        _culture = culture;
    }

    /// <summary>
    /// 異常の通知の文面を作る。
    /// </summary>
    /// <param name="anomaly">異常。</param>
    /// <returns>見出し・本文・押したときに開くもの。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="anomaly"/> が null。</exception>
    public AnomalyToastContent Compose(AnomalyRecord anomaly)
    {
        ArgumentNullException.ThrowIfNull(anomaly);

        var part = PartName(anomaly.HardwareKind);
        var title = string.Format(_culture, TitleTemplate(anomaly), part);
        var items = new List<string>();

        if (Evidence(anomaly, AnomalyEvidenceNames.PeakTemperatureCelsius) is { } temperature)
        {
            items.Add(string.Format(_culture, _templates.Temperature, Round(temperature)));
        }

        if (anomaly.Kind == AnomalyKind.ThermalThrottling
            && Evidence(anomaly, AnomalyEvidenceNames.ClockDropPercent) is { } drop
            && drop >= MinimumClockDropPercent)
        {
            items.Add(string.Format(_culture, _templates.ClockDrop, Round(drop)));
        }

        if (anomaly.Kind == AnomalyKind.WorseThanUsual && Evidence(anomaly, AnomalyEvidenceNames.DeltaFromBaseline) is { } delta && delta > 0)
        {
            items.Add(string.Format(_culture, _templates.DeltaFromUsual, Round(delta)));
        }

        items.Add(DurationText(anomaly.Duration));
        return new AnomalyToastContent(title, string.Join(_templates.Separator, items), NotificationActivationRequest.ForAnomaly(anomaly.AnomalyId));
    }

    private string TitleTemplate(AnomalyRecord anomaly) => anomaly.Kind switch
    {
        AnomalyKind.WorseThanUsual => _templates.WorseThanUsualTitle,
        _ when IsLimitOnly(anomaly) => _templates.LimitThrottlingTitle,
        _ => _templates.ThermalThrottlingTitle,
    };

    /// <summary>
    /// 熱の制限の印も注意の温度以上の時間もなく、電力・電流の制限だけがかかっていたかどうか
    /// （クロックが大きく下がって通知の対象になった場合）。理由の数値がなければ熱とみなす。
    /// </summary>
    private static bool IsLimitOnly(AnomalyRecord anomaly) =>
        Evidence(anomaly, AnomalyEvidenceNames.ThermalLimitSeconds) is 0.0
        && Evidence(anomaly, AnomalyEvidenceNames.AboveThresholdSeconds) is 0.0
        && (Evidence(anomaly, AnomalyEvidenceNames.PowerLimitSeconds) > 0 || Evidence(anomaly, AnomalyEvidenceNames.CurrentLimitSeconds) > 0);

    private string PartName(HardwareKind kind) => kind switch
    {
        HardwareKind.Cpu => _templates.CpuName,
        HardwareKind.Gpu => _templates.GpuName,
        _ => _templates.OtherPartName,
    };

    private string DurationText(TimeSpan duration)
    {
        if (duration < TimeSpan.FromMinutes(1))
        {
            return string.Format(_culture, _templates.DurationSeconds, Math.Max(1, (int)Math.Round(duration.TotalSeconds)));
        }

        return string.Format(_culture, _templates.DurationMinutes, (int)Math.Floor(duration.TotalMinutes));
    }

    private static double? Evidence(AnomalyRecord anomaly, string name) =>
        anomaly.Evidence.TryGetValue(name, out var value) && double.IsFinite(value) ? value : null;

    private static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}

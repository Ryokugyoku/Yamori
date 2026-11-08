using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Yamori.Controls;
using Yamori.Core.Analysis.Parts;
using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Explanation;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Display;
using Yamori.Core.Sensors.TimeSeries;
using Yamori.Localization;

namespace Yamori.Presentation.Analysis;

/// <summary>
/// 分析画面の表示内容（部品の名前、異常の一覧の行、判断の理由、集計値、グラフ）を、表示言語の文字列で作る。
/// </summary>
/// <remarks>文字列は作った時点の言語で決まる。言語を変えたら画面ごと作り直す。</remarks>
public sealed class AnalysisPresenter
{
    /// <summary>グラフに描く点の区切りの数（だいたいグラフの横幅のピクセル数）。</summary>
    public const int ChartBucketCount = 720;

    private readonly AnomalyExplanationComposer _composer;
    private readonly CultureInfo _culture;

    /// <summary>
    /// 今の表示言語で作る。
    /// </summary>
    public AnalysisPresenter()
    {
        _culture = CultureInfo.CurrentUICulture;
        _composer = new AnomalyExplanationComposer(CreateTemplates(), _culture);
    }

    /// <summary>
    /// 左の一覧の行を作る（「すべての異常」と、常に出す部品、異常があるときだけ出す部品）。
    /// </summary>
    /// <returns>行。</returns>
    public IReadOnlyList<AnalysisPartItem> CreatePartItems() =>
        new[] { new AnalysisPartItem(null, UiText.Get("Analysis_AllAnomalies"), "") }
            .Concat(AnalysisParts.Standard.Concat(AnalysisParts.Optional).Select(p => new AnalysisPartItem(p, PartName(p), Glyph(p))))
            .ToArray();

    /// <summary>
    /// 部品の種類の名前を取得する。
    /// </summary>
    /// <param name="part">部品の種類。</param>
    /// <returns>名前。</returns>
    public static string PartName(AnalysisPart part) => UiText.Get($"Analysis_Part_{part}");

    /// <summary>
    /// 部品の種類のアイコンを取得する。
    /// </summary>
    /// <param name="part">部品の種類。</param>
    /// <returns>アイコンの文字（Segoe Fluent Icons）。</returns>
    public static string Glyph(AnalysisPart part) => part switch
    {
        AnalysisPart.Cpu => "",
        AnalysisPart.Gpu => "",
        AnalysisPart.Memory => "",
        AnalysisPart.Storage => "",
        AnalysisPart.Network => "",
        _ => "",
    };

    /// <summary>
    /// 数の札の文字を作る。
    /// </summary>
    /// <param name="count">数。</param>
    /// <returns>札の文字。</returns>
    public string CountText(int count) => count.ToString(_culture);

    /// <summary>
    /// 異常の一覧の行を作る。
    /// </summary>
    /// <param name="anomaly">異常。</param>
    /// <returns>行。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="anomaly"/> が null。</exception>
    public AnomalyItem CreateItem(AnomalyRecord anomaly)
    {
        ArgumentNullException.ThrowIfNull(anomaly);

        var title = Title(anomaly);
        var part = AnalysisParts.From(anomaly.HardwareKind) is { } p ? PartName(p) : anomaly.HardwareKind.ToString();
        var partText = UiText.Format("Analysis_PartAndName", part, anomaly.HardwareName);
        var time = TimeRangeText(anomaly);
        var duration = _composer.FormatDuration(anomaly.Duration);
        if (anomaly.IsOngoing)
        {
            duration = UiText.Format("Analysis_Ongoing", duration);
        }

        var severity = _composer.SeverityName(anomaly.Severity);
        var dismissed = anomaly.IsDismissed ? UiText.Get("Analysis_DismissedBadge") : string.Empty;
        return new AnomalyItem(
            anomaly,
            title,
            partText,
            time,
            duration,
            severity,
            LevelOf(anomaly.Severity),
            dismissed,
            string.Join(", ", new[] { title, severity, partText, time, duration, dismissed }.Where(s => s.Length > 0)));
    }

    /// <summary>
    /// 異常の見出しを作る。電力・電流の制限だけのスロットリングは、熱ではないことが分かる見出しにする。
    /// </summary>
    /// <param name="anomaly">異常。</param>
    /// <returns>見出し。</returns>
    public static string Title(AnomalyRecord anomaly)
    {
        ArgumentNullException.ThrowIfNull(anomaly);
        return AnomalyEvidence.IsLimitOnlyThrottling(anomaly)
            ? UiText.Get("Analysis_Kind_LimitThrottling")
            : anomaly.Kind switch
            {
                AnomalyKind.ThermalThrottling => UiText.Get("Analysis_Kind_ThermalThrottling"),
                AnomalyKind.WorseThanUsual => UiText.Get("Analysis_Kind_WorseThanUsual"),
                _ => UiText.Get("Analysis_Kind_Other"),
            };
    }

    /// <summary>
    /// 判断の理由の一文を作る。
    /// </summary>
    /// <param name="anomaly">異常。</param>
    /// <returns>一文。</returns>
    public string Explain(AnomalyRecord anomaly) => _composer.Compose(anomaly);

    /// <summary>
    /// 集計値の表の行を作る。
    /// </summary>
    /// <param name="analysis">部品の記録と集計値。</param>
    /// <returns>記録のある値の種類ごとの行。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="analysis"/> が null。</exception>
    public IReadOnlyList<AnalysisStatItem> CreateStatItems(PartAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        return analysis.Series
            .Where(s => s.Statistics is not null)
            .Select(s =>
            {
                var stats = s.Statistics!;
                var unit = UnitOf(s.Kind);
                return new AnalysisStatItem(
                    SeriesName(s.Kind),
                    SensorValueFormatter.Format(stats.Minimum, unit, _culture),
                    SensorValueFormatter.Format(stats.Average, unit, _culture),
                    SensorValueFormatter.Format(stats.Maximum, unit, _culture),
                    SensorValueFormatter.Format(stats.Percentile95, unit, _culture));
            })
            .ToArray();
    }

    /// <summary>
    /// 「高温の時間」の説明を作る。
    /// </summary>
    /// <param name="analysis">部品の記録と集計値。</param>
    /// <returns>説明。温度の記録がなければ空。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="analysis"/> が null。</exception>
    public string HighTemperatureText(PartAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        if (analysis.Find(AnalysisSeriesKind.Temperature)?.Statistics is not { AboveThreshold: { } above } stats
            || analysis.TemperatureThresholdCelsius is not { } threshold)
        {
            return string.Empty;
        }

        var ratio = stats.Observed > TimeSpan.Zero ? above / stats.Observed * 100 : 0;
        return UiText.Format("Analysis_HighTemperature", _composer.FormatDuration(above), Math.Round(threshold), ratio);
    }

    /// <summary>
    /// どの記録を使ったかの説明を作る。
    /// </summary>
    /// <param name="analysis">部品の記録と集計値。</param>
    /// <returns>説明。記録がなければ、記録がないことの説明。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="analysis"/> が null。</exception>
    public static string SourceText(PartAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        if (!analysis.HasData)
        {
            return UiText.Get("Analysis_NoRecord");
        }

        var resolutions = analysis.Series.Where(s => s.HasData).SelectMany(s => s.Resolutions).Distinct().ToArray();
        return resolutions.Length > 1
            ? UiText.Get("Analysis_Source_Mixed")
            : UiText.Get(resolutions[0] == TimeSeriesResolution.OneSecond ? "Analysis_Source_Second" : "Analysis_Source_Minute");
    }

    /// <summary>
    /// グラフに描く内容を作る。点は <see cref="ChartBucketCount"/> 個までに減らす。
    /// </summary>
    /// <param name="analysis">部品の記録と集計値。</param>
    /// <param name="bands">色をつける異常。</param>
    /// <param name="selectedAnomalyId">選んでいる異常の番号。なければ null。</param>
    /// <returns>グラフの内容。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public ChartData CreateChart(PartAnalysis analysis, IReadOnlyList<AnomalyRecord> bands, long? selectedAnomalyId)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(bands);

        var range = analysis.Range;
        var bucket = TimeSpan.FromTicks(Math.Max(1, range.Length.Ticks / ChartBucketCount));
        var series = new List<ChartSeries>();
        var leftUsed = false;
        var rightUsed = false;
        ChartReferenceLine? reference = null;
        foreach (var s in analysis.Series.Where(s => s.HasData))
        {
            var unit = UnitOf(s.Kind);
            var side = s.Kind switch
            {
                AnalysisSeriesKind.Load when !leftUsed => ChartAxisSide.Left,
                AnalysisSeriesKind.Temperature when !rightUsed => ChartAxisSide.Right,
                _ when !leftUsed => ChartAxisSide.Left,
                _ => ChartAxisSide.Hidden,
            };
            leftUsed |= side == ChartAxisSide.Left;
            rightUsed |= side == ChartAxisSide.Right;

            // 記録が途切れたところで線を切る。1 分集計がまざるときは、その間隔までつなぐ。
            var gap = TimeSpan.FromTicks(Math.Max(s.CoarsestStep.Ticks * 3, bucket.Ticks * 2));
            var threshold = s.Kind == AnalysisSeriesKind.Temperature ? analysis.TemperatureThresholdCelsius : null;
            var points = s.Points;
            series.Add(new ChartSeries(
                SeriesName(s.Kind),
                SeriesDownsampler.Downsample(points, range, ChartBucketCount),
                ChartAxisScales.For(s.Kind, points, threshold),
                v => SensorValueFormatter.Format(v, unit, _culture),
                (int)s.Kind,
                side,
                gap));

            if (threshold is { } t)
            {
                reference = new ChartReferenceLine(t, series.Count - 1, UiText.Format("Analysis_ThresholdLine", Math.Round(t)));
            }
        }

        var chartBands = bands
            .Select(a => new ChartBand(a.StartedUtc, a.EndedUtc ?? a.LastSeenUtc, a.Severity == AnomalySeverity.Critical, a.AnomalyId == selectedAnomalyId))
            .ToArray();
        return new ChartData(range, series, chartBands, reference);
    }

    /// <summary>
    /// 範囲の説明を作る（例：11/01 09:00〜11/08 09:00）。
    /// </summary>
    /// <param name="range">範囲。</param>
    /// <returns>説明。</returns>
    public string RangeText(AnalysisRange range) =>
        UiText.Format(
            "Analysis_TimeRange",
            ChartTimeLabelFormatter.Point(range.FromUtc.ToLocalTime(), false, _culture),
            ChartTimeLabelFormatter.Point(range.ToUtc.ToLocalTime(), false, _culture));

    private string TimeRangeText(AnomalyRecord anomaly)
    {
        var start = anomaly.StartedUtc.ToLocalTime();
        var end = (anomaly.EndedUtc ?? anomaly.LastSeenUtc).ToLocalTime();
        var startText = ChartTimeLabelFormatter.Point(start, false, _culture);
        var endText = start.Date == end.Date
            ? end.ToString(_culture.DateTimeFormat.ShortTimePattern, _culture)
            : ChartTimeLabelFormatter.Point(end, false, _culture);
        return UiText.Format("Analysis_TimeRange", startText, endText);
    }

    private static string SeriesName(AnalysisSeriesKind kind) => UiText.Get($"Analysis_Series_{kind}");

    private static SensorUnit UnitOf(AnalysisSeriesKind kind) => kind switch
    {
        AnalysisSeriesKind.Temperature => SensorUnit.Celsius,
        AnalysisSeriesKind.Clock => SensorUnit.Hertz,
        _ => SensorUnit.Percent,
    };

    private static StatusLevel LevelOf(AnomalySeverity severity) => severity switch
    {
        AnomalySeverity.Critical => StatusLevel.Critical,
        AnomalySeverity.Warning => StatusLevel.Caution,
        _ => StatusLevel.Attention,
    };

    private static AnomalyExplanationTemplates CreateTemplates() =>
        new(
            UiText.Get("Explain_Sentence"),
            UiText.Get("Explain_SentenceWithoutReasons"),
            UiText.Get("Explain_Separator"),
            UiText.Get("Explain_LastSeparator"),
            UiText.Get("Explain_Severity_Notice"),
            UiText.Get("Explain_Severity_Warning"),
            UiText.Get("Explain_Severity_Critical"),
            UiText.Get("Explain_ThermalLimit"),
            UiText.Get("Explain_AboveThreshold"),
            UiText.Get("Explain_PowerLimit"),
            UiText.Get("Explain_CurrentLimit"),
            UiText.Get("Explain_PeakTemperature"),
            UiText.Get("Explain_ClockDrop"),
            UiText.Get("Explain_DeltaFromUsual"),
            UiText.Get("Explain_IdleLoadDeltaFromUsual"),
            UiText.Get("Explain_Temperature"),
            UiText.Get("Explain_DurationSeconds"),
            UiText.Get("Explain_DurationMinutes"),
            UiText.Get("Explain_DurationHours"));
}

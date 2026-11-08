using System.Globalization;
using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Explanation;
using Yamori.Core.Sensors;

namespace Yamori.Core.Tests.Anomalies;

public class AnomalyExplanationTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private static readonly AnomalyExplanationTemplates Japanese = new(
        Sentence: "{0} だったため、「{1}」と判断しました。",
        SentenceWithoutReasons: "「{0}」と判断しました。",
        Separator: "、",
        LastSeparator: "、",
        SeverityNotice: "注意",
        SeverityWarning: "警告",
        SeverityCritical: "危険",
        ThermalLimit: "熱による制限の印が {0}",
        AboveThreshold: "注意の温度（{1}）以上が {0}",
        PowerLimit: "電力の上限による制限が {0}",
        CurrentLimit: "電流の上限による制限が {0}",
        PeakTemperature: "最高温度が {0}",
        ClockDrop: "クロックの低下が約 {0}%",
        DeltaFromUsual: "待機中の温度がふだん（直近 14 日の中央値 {0}）より {1} 高い時間が {2}",
        IdleLoadDeltaFromUsual: "待機中の負荷がふだん（直近 14 日の中央値 {0}%）より {1} ポイント高い時間が {2}",
        Temperature: "{0}℃",
        DurationSeconds: "{0} 秒",
        DurationMinutes: "{0} 分 {1} 秒",
        DurationHours: "{0} 時間 {1} 分");

    private static readonly AnomalyExplanationTemplates English = Japanese with
    {
        Sentence = "Rated “{1}” because of {0}.",
        Separator = ", ",
        LastSeparator = " and ",
        SeverityWarning = "Warning",
        ThermalLimit = "thermal limiting for {0}",
        PeakTemperature = "a peak of {0}",
        DurationSeconds = "{0} s",
    };

    [Fact]
    public void ThrottlingIsExplainedInOneSentenceWithTheEvidence()
    {
        var anomaly = Anomaly(
            AnomalyKind.ThermalThrottling,
            (AnomalyEvidenceNames.ThermalLimitSeconds, 42),
            (AnomalyEvidenceNames.AboveThresholdSeconds, 70),
            (AnomalyEvidenceNames.ThresholdCelsius, 95),
            (AnomalyEvidenceNames.PowerLimitSeconds, 0),
            (AnomalyEvidenceNames.PeakTemperatureCelsius, 96.6),
            (AnomalyEvidenceNames.ClockDropPercent, 12.4));

        var text = new AnomalyExplanationComposer(Japanese, CultureInfo.InvariantCulture).Compose(anomaly);

        Assert.Equal(
            "熱による制限の印が 42 秒、注意の温度（95℃）以上が 1 分 10 秒、最高温度が 97℃、クロックの低下が約 12% だったため、「警告」と判断しました。",
            text);
    }

    [Fact]
    public void EnglishUsesAndBeforeTheLastItemAndSmallDropsAreLeftOut()
    {
        var anomaly = Anomaly(
            AnomalyKind.ThermalThrottling,
            (AnomalyEvidenceNames.ThermalLimitSeconds, 12),
            (AnomalyEvidenceNames.PeakTemperatureCelsius, 99),
            (AnomalyEvidenceNames.ClockDropPercent, 3));

        var text = new AnomalyExplanationComposer(English, CultureInfo.InvariantCulture).Compose(anomaly);

        Assert.Equal("Rated “Warning” because of thermal limiting for 12 s and a peak of 99℃.", text);
    }

    [Fact]
    public void OtherKindsAndMissingEvidenceStillGiveASentence()
    {
        var composer = new AnomalyExplanationComposer(Japanese, CultureInfo.InvariantCulture);
        var worse = Anomaly(AnomalyKind.WorseThanUsual, (AnomalyEvidenceNames.BaselineValue, 41), (AnomalyEvidenceNames.DeltaFromBaseline, 9.4));

        Assert.Equal("待機中の温度がふだん（直近 14 日の中央値 41℃）より 9℃ 高い時間が 3 分 0 秒 だったため、「警告」と判断しました。", composer.Compose(worse));
        Assert.Equal("「危険」と判断しました。", composer.Compose(Anomaly(AnomalyKind.ThermalThrottling) with { Severity = AnomalySeverity.Critical }));
        Assert.Equal("2 時間 5 分", composer.FormatDuration(TimeSpan.FromMinutes(125)));
    }

    [Fact]
    public void WorseThanUsualExplainsTemperatureAndIdleLoadWithTheUsualMedianAndDuration()
    {
        var composer = new AnomalyExplanationComposer(Japanese, CultureInfo.InvariantCulture);
        var both = Anomaly(
            AnomalyKind.WorseThanUsual,
            (AnomalyEvidenceNames.BaselineValue, 42),
            (AnomalyEvidenceNames.DeltaFromBaseline, 9.2),
            (AnomalyEvidenceNames.BaselineIdleLoadPercent, 3.4),
            (AnomalyEvidenceNames.IdleLoadDeltaPoints, 12.6)) with { EndedUtc = null, LastSeenUtc = Start.AddHours(3), Severity = AnomalySeverity.Notice };

        Assert.Equal(
            "待機中の温度がふだん（直近 14 日の中央値 42℃）より 9℃ 高い時間が 3 時間 0 分、待機中の負荷がふだん（直近 14 日の中央値 3%）より 13 ポイント高い時間が 3 時間 0 分 だったため、「注意」と判断しました。",
            composer.Compose(both));
    }

    [Fact]
    public void LimitOnlyThrottlingIsRecognized()
    {
        var limit = Anomaly(
            AnomalyKind.ThermalThrottling,
            (AnomalyEvidenceNames.ThermalLimitSeconds, 0),
            (AnomalyEvidenceNames.AboveThresholdSeconds, 0),
            (AnomalyEvidenceNames.CurrentLimitSeconds, 60));

        Assert.True(AnomalyEvidence.IsLimitOnlyThrottling(limit));
        Assert.False(AnomalyEvidence.IsLimitOnlyThrottling(Anomaly(AnomalyKind.ThermalThrottling)));
        Assert.False(AnomalyEvidence.IsLimitOnlyThrottling(limit with { Kind = AnomalyKind.WorseThanUsual }));
    }

    private static AnomalyRecord Anomaly(AnomalyKind kind, params (string Name, double Value)[] evidence) =>
        new(1, kind, "cpu/0", HardwareKind.Cpu, "Intel Core i7", Start, Start.AddMinutes(3), Start.AddMinutes(3), AnomalySeverity.Warning,
            evidence.ToDictionary(e => e.Name, e => e.Value, StringComparer.Ordinal), null, null);
}

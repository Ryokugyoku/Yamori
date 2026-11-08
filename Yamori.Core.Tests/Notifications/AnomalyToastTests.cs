using System.Globalization;
using Yamori.Core.Anomalies;
using Yamori.Core.Notifications;
using Yamori.Core.Sensors;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Notifications;

public class AnomalyToastTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private static readonly AnomalyToastTemplates Japanese = new(
        ThermalThrottlingTitle: "{0} が熱で性能を落としています",
        LimitThrottlingTitle: "{0} が電力の上限で性能を落としています",
        WorseThanUsualTitle: "{0} の温度がふだんより高くなっています",
        CpuName: "CPU",
        GpuName: "GPU",
        OtherPartName: "PC",
        Temperature: "{0}℃",
        ClockDrop: "クロック約 {0}% 低下",
        DeltaFromUsual: "ふだんより {0}℃ 高い",
        DurationSeconds: "{0} 秒間",
        DurationMinutes: "{0} 分間",
        Separator: "・");

    [Fact]
    public void ThrottlingToastShowsPartPhenomenonAndNumbersWithoutTheAppName()
    {
        var anomaly = Anomaly(
            7,
            TimeSpan.FromMinutes(12.6),
            (AnomalyEvidenceNames.PeakTemperatureCelsius, 95.6),
            (AnomalyEvidenceNames.ClockDropPercent, 29.5),
            (AnomalyEvidenceNames.ThermalLimitSeconds, 40));

        var content = new AnomalyToastComposer(Japanese, CultureInfo.InvariantCulture).Compose(anomaly);

        Assert.Equal("CPU が熱で性能を落としています", content.Title);
        Assert.Equal("96℃・クロック約 30% 低下・12 分間", content.Body);
        Assert.Equal(NotificationActivationRequest.ForAnomaly(7), content.Activation);
        Assert.DoesNotContain("Yamori", content.Title + content.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SmallClockDropsAreLeftOutAndShortEpisodesAreInSeconds()
    {
        var anomaly = Anomaly(
            1,
            TimeSpan.FromSeconds(42),
            (AnomalyEvidenceNames.PeakTemperatureCelsius, 95),
            (AnomalyEvidenceNames.ClockDropPercent, 2),
            (AnomalyEvidenceNames.AboveThresholdSeconds, 30));

        var content = new AnomalyToastComposer(Japanese, CultureInfo.InvariantCulture).Compose(anomaly);

        Assert.Equal("95℃・42 秒間", content.Body);
    }

    [Fact]
    public void LimitOnlyEpisodesSayItIsTheLimitAndOtherPartsUseTheGenericName()
    {
        var limit = Anomaly(
            2,
            TimeSpan.FromMinutes(3),
            (AnomalyEvidenceNames.ThermalLimitSeconds, 0),
            (AnomalyEvidenceNames.AboveThresholdSeconds, 0),
            (AnomalyEvidenceNames.CurrentLimitSeconds, 180),
            (AnomalyEvidenceNames.ClockDropPercent, 15));
        var zone = Anomaly(3, TimeSpan.FromMinutes(1), (AnomalyEvidenceNames.PeakTemperatureCelsius, 88)) with { HardwareKind = HardwareKind.ThermalZone };
        var composer = new AnomalyToastComposer(Japanese, CultureInfo.InvariantCulture);

        Assert.Equal("CPU が電力の上限で性能を落としています", composer.Compose(limit).Title);
        Assert.Equal("クロック約 15% 低下・3 分間", composer.Compose(limit).Body);
        Assert.Equal("PC が熱で性能を落としています", composer.Compose(zone).Title);
    }

    [Fact]
    public void WorseThanUsualShowsTheDifference()
    {
        var anomaly = Anomaly(4, TimeSpan.FromMinutes(20), (AnomalyEvidenceNames.PeakTemperatureCelsius, 61), (AnomalyEvidenceNames.DeltaFromBaseline, 9.4))
            with { Kind = AnomalyKind.WorseThanUsual, HardwareKind = HardwareKind.Gpu };

        var content = new AnomalyToastComposer(Japanese, CultureInfo.InvariantCulture).Compose(anomaly);

        Assert.Equal("GPU の温度がふだんより高くなっています", content.Title);
        Assert.Equal("61℃・ふだんより 9℃ 高い・20 分間", content.Body);
    }

    [Theory]
    [InlineData(AnomalyChange.Started, true, false, true)]
    [InlineData(AnomalyChange.Escalated, true, false, true)]
    [InlineData(AnomalyChange.Ended, true, false, false)]
    [InlineData(AnomalyChange.DismissalChanged, true, false, false)]
    [InlineData(AnomalyChange.Started, false, false, false)]
    [InlineData(AnomalyChange.Started, true, true, false)]
    public void OnlyNotifiableStartsAndEscalationsAreCandidates(AnomalyChange change, bool shouldNotify, bool dismissed, bool expected)
    {
        var anomaly = Anomaly(1, TimeSpan.FromMinutes(1)) with { DismissedUtc = dismissed ? Start : null };

        Assert.Equal(expected, AnomalyToastFilter.IsCandidate(new AnomalyNotice(change, anomaly, shouldNotify)));
    }

    [Fact]
    public void FilterFollowsTheSettingsAndShowsTheSameAnomalyOnceInSixHours()
    {
        var filter = new AnomalyToastFilter();
        var first = Notice(Anomaly(1, TimeSpan.FromMinutes(1)));
        var on = AnomalySettingRules.Default(AnomalyKind.ThermalThrottling);
        var off = on with { NotificationsEnabled = false };

        Assert.True(filter.Allows(first, notificationsEnabled: true, on, Start));
        Assert.True(filter.Allows(first, notificationsEnabled: null, kindSetting: null, Start));
        Assert.False(filter.Allows(first, notificationsEnabled: false, on, Start));
        Assert.False(filter.Allows(first, notificationsEnabled: true, off, Start));

        filter.RecordShown(first.Anomaly, Start);

        // 同じ異常（重くなった知らせ）も、同じ部品の次の異常も、6 時間は出さない。別の部品は出す。
        Assert.False(filter.Allows(first with { Change = AnomalyChange.Escalated }, true, on, Start.AddMinutes(1)));
        var second = Notice(Anomaly(2, TimeSpan.FromMinutes(1)));
        Assert.False(filter.Allows(second, true, on, Start.AddHours(5)));
        Assert.True(filter.Allows(Notice(Anomaly(3, TimeSpan.FromMinutes(1)) with { HardwareKey = "gpu/0" }), true, on, Start.AddHours(1)));
        Assert.True(filter.Allows(second, true, on, Start.AddHours(6)));
        Assert.False(filter.Allows(first, true, on, Start.AddHours(7)));
    }

    [Fact]
    public async Task DispatcherShowsReadsSettingsAndRecordsTheNotification()
    {
        var service = new FakeAnomalyService();
        var sink = new RecordingSink();
        var clock = new ManualClock { Now = Start };
        var dispatcher = new AnomalyNotificationDispatcher(service, new AnomalyToastFilter(), sink, clock);
        var notice = Notice(Anomaly(5, TimeSpan.FromMinutes(1)));

        Assert.Equal(AnomalyNotificationOutcome.Shown, await dispatcher.HandleAsync(notice));
        Assert.Equal([5L], sink.Shown);
        Assert.Equal([5L], service.NotifiedCalls);

        // 同じ異常の知らせがもう一度来ても出さない。
        Assert.Equal(AnomalyNotificationOutcome.Suppressed, await dispatcher.HandleAsync(notice with { Change = AnomalyChange.Escalated }));
        Assert.Equal(AnomalyNotificationOutcome.NotCandidate, await dispatcher.HandleAsync(notice with { Change = AnomalyChange.Ended }));
        Assert.Single(sink.Shown);
    }

    [Fact]
    public async Task DispatcherKeepsQuietWhenSettingsAreOffOrTheUserIsBusy()
    {
        var service = new FakeAnomalyService { NotificationsEnabled = false };
        var sink = new RecordingSink();
        var dispatcher = new AnomalyNotificationDispatcher(service, new AnomalyToastFilter(), sink, new ManualClock { Now = Start });

        Assert.Equal(AnomalyNotificationOutcome.Suppressed, await dispatcher.HandleAsync(Notice(Anomaly(1, TimeSpan.FromMinutes(1)))));

        service.NotificationsEnabled = true;
        await service.SetSettingAsync(AnomalySettingRules.Default(AnomalyKind.ThermalThrottling) with { NotificationsEnabled = false });
        Assert.Equal(AnomalyNotificationOutcome.Suppressed, await dispatcher.HandleAsync(Notice(Anomaly(2, TimeSpan.FromMinutes(1)))));

        // 全画面中・一時停止中は控え、通知済みにしない（次の知らせでは出せる）。
        await service.SetSettingAsync(AnomalySettingRules.Default(AnomalyKind.ThermalThrottling));
        sink.Busy = true;
        Assert.Equal(AnomalyNotificationOutcome.Held, await dispatcher.HandleAsync(Notice(Anomaly(3, TimeSpan.FromMinutes(1)))));
        Assert.Empty(service.NotifiedCalls);

        sink.Busy = false;
        Assert.Equal(AnomalyNotificationOutcome.Shown, await dispatcher.HandleAsync(Notice(Anomaly(3, TimeSpan.FromMinutes(2))) with { Change = AnomalyChange.Escalated }));
        Assert.Equal([3L], service.NotifiedCalls);
    }

    private static AnomalyNotice Notice(AnomalyRecord anomaly) => new(AnomalyChange.Started, anomaly, ShouldNotify: true);

    private static AnomalyRecord Anomaly(long id, TimeSpan duration, params (string Name, double Value)[] evidence) =>
        new(id, AnomalyKind.ThermalThrottling, "cpu/0", HardwareKind.Cpu, "Intel Core i7", Start, Start + duration, null, AnomalySeverity.Warning,
            evidence.ToDictionary(e => e.Name, e => e.Value, StringComparer.Ordinal), null, null);

    private sealed class RecordingSink : IAnomalyToastSink
    {
        public List<long> Shown { get; } = [];

        public bool Busy { get; set; }

        public Task<bool> TryShowAsync(AnomalyRecord anomaly)
        {
            if (Busy)
            {
                return Task.FromResult(false);
            }

            Shown.Add(anomaly.AnomalyId);
            return Task.FromResult(true);
        }
    }
}

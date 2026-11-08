using Yamori.Core.Anomalies;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Ipc;
using Yamori.Core.Sensors.Ipc.Client;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Sensors;

public class AnomalyPipeTests
{
    private static readonly DateTimeOffset Time = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly SensorKey Load = WellKnownSensorKeys.CpuTotalLoad;

    [Fact]
    public void ContractVersionIsThree()
    {
        Assert.Equal(3, SensorContract.Version);
        Assert.True(SensorContract.IsCompatible(2));
    }

    [Fact]
    public async Task AnomaliesAndSettingsTravelThroughThePipe()
    {
        await using var harness = new PipeServerHarness(callerIsAdministrator: false);
        harness.Anomalies.Anomalies.Add(Anomaly(7));
        harness.Start();
        await using var remote = new RunningRemote(harness);
        await remote.WaitConnectedAsync();

        var anomalies = await remote.Service.GetAnomaliesAsync(Time.AddDays(-1), Time.AddDays(1), includeDismissed: true, limit: 50);
        var anomaly = Assert.Single(anomalies);
        Assert.Equal(7, anomaly.AnomalyId);
        Assert.Equal(AnomalyKind.ThermalThrottling, anomaly.Kind);
        Assert.Equal("cpu/0", anomaly.HardwareKey);
        Assert.Equal(AnomalySeverity.Warning, anomaly.Severity);
        Assert.Equal(96, anomaly.Evidence[AnomalyEvidenceNames.PeakTemperatureCelsius]);
        Assert.Equal(Time, anomaly.StartedUtc);
        Assert.Null(anomaly.EndedUtc);
        Assert.Equal((Time.AddDays(-1), Time.AddDays(1), true, 50), harness.Anomalies.LastQuery);

        // 管理者でなくても、「これは正常」・通知済み・設定は変えられる。
        Assert.Equal(SensorSettingResult.Applied, await remote.Service.SetDismissedAsync(7, true));
        Assert.Equal(SensorSettingResult.NotFound, await remote.Service.SetDismissedAsync(8, true));
        Assert.Equal(SensorSettingResult.Applied, await remote.Service.MarkNotifiedAsync(7));
        Assert.Equal(SensorSettingResult.Applied, await remote.Service.SetSettingAsync(new AnomalySetting(AnomalyKind.ThermalThrottling, false, 92)));
        Assert.Equal(SensorSettingResult.Invalid, await remote.Service.SetSettingAsync(new AnomalySetting(AnomalyKind.ThermalThrottling, true, 500)));
        Assert.Equal(SensorSettingResult.Invalid, await remote.Service.SetSettingAsync(new AnomalySetting((AnomalyKind)42, true, null)));
        Assert.Equal([(7L, true), (8L, true)], harness.Anomalies.DismissCalls);
        Assert.Equal([7L], harness.Anomalies.NotifiedCalls);

        var settings = await remote.Service.GetSettingsAsync();
        Assert.Contains(new AnomalySetting(AnomalyKind.ThermalThrottling, false, 92), settings);
        Assert.Contains(AnomalySettingRules.Default(AnomalyKind.WorseThanUsual), settings);
    }

    [Fact]
    public async Task BadAnomalyRequestsAreRejected()
    {
        await using var harness = new PipeServerHarness();
        harness.Anomalies.Anomalies.Add(Anomaly(1));
        harness.Start();
        await using var remote = new RunningRemote(harness);
        await remote.WaitConnectedAsync();

        // 期間が長すぎる依頼は断られ、空になる。
        Assert.Empty(await remote.Service.GetAnomaliesAsync(Time.AddDays(-500), Time, false, 10));
        Assert.Empty(await remote.Service.GetAnomaliesAsync(Time, Time.AddDays(-1), false, 10));
        Assert.Equal(SensorSettingResult.Unavailable, await remote.Service.SetDismissedAsync(0, true));
    }

    [Fact]
    public async Task AnomalyNoticesReachClients()
    {
        await using var harness = new PipeServerHarness();
        harness.Start();
        await using var remote = new RunningRemote(harness);
        await remote.WaitConnectedAsync();
        var received = new List<AnomalyNotice>();
        remote.Service.AnomalyChanged += (_, n) => { lock (received) { received.Add(n); } };

        harness.Anomalies.Raise(new AnomalyNotice(AnomalyChange.Started, Anomaly(3), ShouldNotify: true));

        await PipeServerHarness.WaitUntilAsync(() => { lock (received) { return received.Count == 1; } }, "異常の知らせが届くこと");
        Assert.Equal(AnomalyChange.Started, received[0].Change);
        Assert.True(received[0].ShouldNotify);
        Assert.Equal(3, received[0].Anomaly.AnomalyId);
    }

    [Fact]
    public async Task ClientsCanChooseToReceiveOnlyAnomalies()
    {
        await using var harness = new PipeServerHarness();
        harness.Start();
        await using var notifier = new RunningRemote(harness, SensorNotificationTopics.Anomalies);
        await using var gui = new RunningRemote(harness);
        await notifier.WaitConnectedAsync();
        await gui.WaitConnectedAsync();
        await PipeServerHarness.WaitUntilAsync(() => harness.Server!.ConnectionCount == 2, "2 つのクライアントがつながること");

        var notifierReadings = 0;
        var notifierAnomalies = 0;
        var guiReadings = 0;
        var guiAnomalies = 0;
        notifier.Service.ReadingsUpdated += (_, _) => Interlocked.Increment(ref notifierReadings);
        notifier.Service.AnomalyChanged += (_, _) => Interlocked.Increment(ref notifierAnomalies);
        gui.Service.ReadingsUpdated += (_, _) => Interlocked.Increment(ref guiReadings);
        gui.Service.AnomalyChanged += (_, _) => Interlocked.Increment(ref guiAnomalies);

        harness.Service.RaiseReadings([SensorReading.Ok(Load, 10, Time, "win-std")]);
        harness.Anomalies.Raise(new AnomalyNotice(AnomalyChange.Ended, Anomaly(4), ShouldNotify: false));

        await PipeServerHarness.WaitUntilAsync(() => Volatile.Read(ref notifierAnomalies) == 1, "通知専用のクライアントに異常の知らせが届くこと");
        await PipeServerHarness.WaitUntilAsync(() => Volatile.Read(ref guiAnomalies) == 1 && Volatile.Read(ref guiReadings) == 1, "画面には両方届くこと");
        Assert.Equal(0, Volatile.Read(ref notifierReadings));
    }

    [Theory]
    [InlineData(SensorNotificationTopics.All, SensorNotificationKind.ReadingsUpdated, true)]
    [InlineData(SensorNotificationTopics.All, (SensorNotificationKind)99, true)]
    [InlineData(SensorNotificationTopics.Anomalies, SensorNotificationKind.AnomalyChanged, true)]
    [InlineData(SensorNotificationTopics.Anomalies, SensorNotificationKind.ReadingsUpdated, false)]
    [InlineData(SensorNotificationTopics.Anomalies, (SensorNotificationKind)99, false)]
    [InlineData(SensorNotificationTopics.Readings | SensorNotificationTopics.Catalog, SensorNotificationKind.CatalogChanged, true)]
    public void TopicsSelectNotifications(SensorNotificationTopics topics, SensorNotificationKind kind, bool expected)
    {
        Assert.Equal(expected, topics.Includes(kind));
    }

    [Fact]
    public async Task OneSecondHistoryCanBeRequested()
    {
        await using var harness = new PipeServerHarness();
        harness.Service.History.Add(new SensorHistoryPoint(Time, 1, 1, 1, 1));
        harness.Start();
        await using var remote = new RunningRemote(harness);
        await remote.WaitConnectedAsync();

        var points = await remote.Service.GetHistoryAsync(Load, Time.AddMinutes(-1), Time.AddMinutes(1), TimeSeriesResolution.OneSecond);

        Assert.Single(points);
        Assert.Equal(TimeSeriesResolution.OneSecond, harness.Service.HistoryRequests[^1].Resolution);
    }

    private static AnomalyRecord Anomaly(long id) =>
        new(id, AnomalyKind.ThermalThrottling, "cpu/0", HardwareKind.Cpu, "CPU", Time, Time.AddMinutes(1), null, AnomalySeverity.Warning,
            new Dictionary<string, double> { [AnomalyEvidenceNames.PeakTemperatureCelsius] = 96 }, null, null);

    private sealed class RunningRemote : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellation = new();
        private readonly Task _run;

        public RunningRemote(PipeServerHarness harness, SensorNotificationTopics topics = SensorNotificationTopics.All)
        {
            var options = new RemoteSensorServiceOptions
            {
                RetryDelay = PipeServerHarness.FastRetry.RetryDelay,
                IncompatibleRetryDelay = PipeServerHarness.FastRetry.IncompatibleRetryDelay,
                HandshakeTimeout = PipeServerHarness.FastRetry.HandshakeTimeout,
                RequestTimeout = PipeServerHarness.FastRetry.RequestTimeout,
                Topics = topics,
            };
            Service = new RemoteSensorService(harness.Connector, TimeProvider.System, options);
            _run = Service.RunAsync(_cancellation.Token);
        }

        public RemoteSensorService Service { get; }

        public Task WaitConnectedAsync() =>
            PipeServerHarness.WaitUntilAsync(() => Service.ConnectionState == HelperConnectionState.Connected, "Helper につながること");

        public async ValueTask DisposeAsync()
        {
            await _cancellation.CancelAsync();
            await _run.WaitAsync(TimeSpan.FromSeconds(10));
            _cancellation.Dispose();
        }
    }
}

using System.IO.Pipes;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Collection;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Hot;
using Yamori.Core.Sensors.Ipc;
using Yamori.Core.Sensors.Ipc.Client;
using Yamori.Core.Sensors.Registry;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Sensors;

public class SensorPipeTests
{
    private static readonly SensorKey Package = SensorKey.Parse("cpu/0/temperature/package");
    private static readonly SensorKey Load = WellKnownSensorKeys.CpuTotalLoad;
    private static readonly DateTimeOffset Time = new(2026, 11, 7, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RemoteServiceReadsEverythingThroughThePipe()
    {
        await using var harness = new PipeServerHarness();
        harness.Service.Catalog.Add(new SensorDescriptor(Package, SensorUnit.Celsius, "CPU Package", "CPU", "temperatures", "pawnio"));
        harness.Service.Latest[Package] = SensorReading.Ok(Package, 55, Time, "pawnio");
        harness.Service.History.Add(new SensorHistoryPoint(Time, 50, 60, 55, 6));
        harness.Service.Policies[Package] = new SensorLogPolicy(true, TimeSpan.FromSeconds(10), 0.5);
        harness.Service.ExtendedState = ExtendedSensorState.Active;
        harness.Start();
        await using var remote = new RunningRemote(harness);
        await remote.WaitConnectedAsync();

        var catalog = await remote.Service.GetCatalogAsync();
        var latest = await remote.Service.GetLatestAsync([Package, Load]);
        var recent = await remote.Service.GetRecentAsync(Package, TimeSpan.FromMinutes(1));
        var history = await remote.Service.GetHistoryAsync(Package, Time.AddHours(-1), Time.AddHours(1), TimeSeriesResolution.Raw);
        var policy = await remote.Service.GetLogPolicyAsync(Package);
        var state = await remote.Service.GetExtendedSensorStateAsync();

        Assert.Equal(harness.Service.Catalog, catalog);
        Assert.Equal(55, latest[0].Value);
        Assert.Equal(SensorStatus.Unsupported, latest[1].Status);
        Assert.Single(recent);
        Assert.Equal(harness.Service.History, history);
        Assert.Equal(harness.Service.Policies[Package], policy);
        Assert.Equal(ExtendedSensorState.Active, state);
    }

    [Fact]
    public async Task NotificationsReachEveryConnectedClient()
    {
        await using var harness = new PipeServerHarness();
        harness.Start();
        await using var first = new RunningRemote(harness);
        await using var second = new RunningRemote(harness);
        await first.WaitConnectedAsync();
        await second.WaitConnectedAsync();
        await PipeServerHarness.WaitUntilAsync(() => harness.Server!.ConnectionCount == 2, "2 つのクライアントがつながること");

        var received = new List<double?>();
        var states = new List<ExtendedSensorState>();
        second.Service.ReadingsUpdated += (_, r) => { lock (received) { received.AddRange(r.Select(x => x.Value)); } };
        second.Service.ExtendedSensorStateChanged += (_, s) => { lock (states) { states.Add(s); } };
        var firstGot = false;
        first.Service.ReadingsUpdated += (_, _) => firstGot = true;

        harness.Service.RaiseReadings([SensorReading.Ok(Load, 12.5, Time, "win-std")]);
        harness.Service.RaiseExtendedState(ExtendedSensorState.Unavailable);

        await PipeServerHarness.WaitUntilAsync(() => { lock (received) { return received.Contains(12.5); } }, "最新値の通知が届くこと");
        await PipeServerHarness.WaitUntilAsync(() => { lock (states) { return states.Contains(ExtendedSensorState.Unavailable); } }, "状態の通知が届くこと");
        await PipeServerHarness.WaitUntilAsync(() => firstGot, "もう 1 つのクライアントにも届くこと");
    }

    [Fact]
    public async Task SettingsCanOnlyBeChangedByAdministrators()
    {
        await using var harness = new PipeServerHarness(callerIsAdministrator: false);
        harness.Service.Policies[Package] = SensorLogPolicy.Disabled;
        harness.Start();
        await using var remote = new RunningRemote(harness);
        await remote.WaitConnectedAsync();

        Assert.Equal(SensorSettingResult.AccessDenied, await remote.Service.SetLogPolicyAsync(Package, new SensorLogPolicy(true, TimeSpan.FromSeconds(5), 0)));
        Assert.Equal(SensorSettingResult.AccessDenied, await remote.Service.SetPawnIoConsentAsync(true));
        Assert.Equal(0, harness.Service.SetCalls);
        Assert.False(await remote.Service.GetPawnIoConsentAsync());
    }

    [Fact]
    public async Task AdministratorsCanChangeSettings()
    {
        await using var harness = new PipeServerHarness(callerIsAdministrator: true);
        harness.Service.Policies[Package] = SensorLogPolicy.Disabled;
        harness.Start();
        await using var remote = new RunningRemote(harness);
        await remote.WaitConnectedAsync();

        Assert.Equal(SensorSettingResult.Applied, await remote.Service.SetLogPolicyAsync(Package, new SensorLogPolicy(true, TimeSpan.FromSeconds(5), 0)));
        Assert.Equal(SensorSettingResult.NotFound, await remote.Service.SetLogPolicyAsync(Load, SensorLogPolicy.Disabled));
        Assert.Equal(SensorSettingResult.Applied, await remote.Service.SetPawnIoConsentAsync(true));
        Assert.True(await remote.Service.GetPawnIoConsentAsync());
        Assert.True(harness.Service.Policies[Package].Enabled);
    }

    [Fact]
    public async Task ServerRejectsOldClientsAndUnknownOperations()
    {
        await using var harness = new PipeServerHarness();
        harness.Start();

        await using (var old = new IpcConnection(await harness.Connector(CancellationToken.None)))
        {
            await old.SendAsync(new IpcEnvelope { Kind = IpcMessageKind.Hello, Version = SensorContract.MinimumCompatibleVersion - 1 }, CancellationToken.None);
            var reply = await old.ReceiveAsync(CancellationToken.None);

            Assert.False(reply!.Accepted);
            Assert.Equal(SensorContract.Version, reply.Version);
            Assert.Null(await old.ReceiveAsync(CancellationToken.None));
        }

        await using var client = await SensorPipeClient.ConnectAsync(await harness.Connector(CancellationToken.None), TimeSpan.FromSeconds(2), CancellationToken.None);
        client.Start();
        var unknown = await client.RequestAsync((SensorOperation)999, null, CancellationToken.None);
        var invalid = await client.RequestAsync(SensorOperation.GetLatest, null, CancellationToken.None);

        Assert.Equal(IpcResponseStatus.UnknownOperation, unknown.Status);
        Assert.Equal(IpcResponseStatus.InvalidRequest, invalid.Status);
    }

    [Fact]
    public async Task ClientRefusesIncompatibleServer()
    {
        var name = "yamori-test-" + Guid.NewGuid().ToString("N");
        await using var pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var serve = Task.Run(async () =>
        {
            await pipe.WaitForConnectionAsync();
            var connection = new IpcConnection(pipe);
            await connection.ReceiveAsync(CancellationToken.None);
            await connection.SendAsync(new IpcEnvelope { Kind = IpcMessageKind.HelloReply, Version = 0, Accepted = true }, CancellationToken.None);
        });

        var stream = await SensorPipeConnector.Create(name, TimeSpan.FromSeconds(2))(CancellationToken.None);
        var error = await Assert.ThrowsAsync<SensorContractMismatchException>(() => SensorPipeClient.ConnectAsync(stream, TimeSpan.FromSeconds(2), CancellationToken.None));

        Assert.Equal(0, error.PeerVersion);
        await serve;
    }

    [Fact]
    public async Task RemoteReconnectsAfterHelperRestarts()
    {
        await using var harness = new PipeServerHarness();
        harness.Service.Catalog.Add(new SensorDescriptor(Load, SensorUnit.Percent, "Total", "CPU", "load", "win-std"));
        await using var remote = new RunningRemote(harness);

        await PipeServerHarness.WaitUntilAsync(() => remote.Service.ConnectionState == HelperConnectionState.NotRunning, "Helper がいないと NotRunning になること");
        Assert.Empty(await remote.Service.GetCatalogAsync());
        Assert.Equal(SensorStatus.TemporarilyUnavailable, (await remote.Service.GetLatestAsync([Load]))[0].Status);
        Assert.Equal(SensorSettingResult.Unavailable, await remote.Service.SetPawnIoConsentAsync(true));

        var catalogs = 0;
        remote.Service.CatalogChanged += (_, c) => { if (c.Count == 1) { Interlocked.Increment(ref catalogs); } };
        harness.Start();
        await remote.WaitConnectedAsync();
        await PipeServerHarness.WaitUntilAsync(() => catalogs >= 1, "つないだらカタログを通知すること");

        await harness.StopAsync();
        await PipeServerHarness.WaitUntilAsync(() => remote.Service.ConnectionState == HelperConnectionState.NotRunning, "Helper が止まったら NotRunning になること");

        harness.Start();
        await remote.WaitConnectedAsync();
        await PipeServerHarness.WaitUntilAsync(() => catalogs >= 2, "つなぎ直したらカタログを通知し直すこと");
        Assert.Single(await remote.Service.GetCatalogAsync());
    }

    [Fact]
    public async Task FailoverShowsStandardSensorsWhileHelperIsDownAndSwitchesBack()
    {
        await using var harness = new PipeServerHarness();
        harness.Service.Catalog.Add(new SensorDescriptor(Package, SensorUnit.Celsius, "CPU Package", "CPU", "temperatures", "pawnio"));
        var remote = new RemoteSensorService(harness.Connector, TimeProvider.System, PipeServerHarness.FastRetry);
        var fallbacks = 0;
        using var failover = new FailoverSensorService(remote, () =>
        {
            Interlocked.Increment(ref fallbacks);
            return CreateFallback();
        });
        IReadOnlyList<SensorDescriptor> lastCatalog = [];
        failover.CatalogChanged += (_, c) => Volatile.Write(ref lastCatalog, c);
        using var cancellation = new CancellationTokenSource();
        var run = failover.RunAsync(cancellation.Token);

        await PipeServerHarness.WaitUntilAsync(() => failover.IsUsingFallback, "Helper がいない間は代わりの表示を使うこと");
        await PipeServerHarness.WaitUntilAsync(() => Volatile.Read(ref lastCatalog).Any(d => d.Key == Load), "代わりの表示は標準 API のセンサーを出すこと");
        Assert.Equal(ExtendedSensorState.HelperRequired, await failover.GetExtendedSensorStateAsync());
        Assert.Empty(await failover.GetHistoryAsync(Load, Time, Time.AddHours(1), TimeSeriesResolution.Raw));

        harness.Start();
        await PipeServerHarness.WaitUntilAsync(() => failover.ConnectionState == HelperConnectionState.Connected && !failover.IsUsingFallback, "Helper が動いたら切り替えること");
        await PipeServerHarness.WaitUntilAsync(() => Volatile.Read(ref lastCatalog).Any(d => d.Key == Package), "Helper のカタログに切り替えること");

        await harness.StopAsync();
        await PipeServerHarness.WaitUntilAsync(() => failover.IsUsingFallback, "Helper が止まったら代わりの表示に戻ること");
        Assert.Equal(2, fallbacks);

        await cancellation.CancelAsync();
        await run.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(failover.IsUsingFallback);
    }

    private static LocalSensorService CreateFallback()
    {
        var provider = new FakeSensorProvider("win-std").Declare(Load, 10);
        var registry = new SensorRegistry([provider], new SensorPriorityPolicy(SensorPriorityOptions.Default), TimeProvider.System);
        var hot = new InMemoryHotStore(TimeProvider.System);
        var loop = new SensorCollectionLoop(registry, hot, new NoChanges(), TimeProvider.System, TimeSpan.FromMilliseconds(20));
        return new LocalSensorService(registry, hot, loop, null, new OutsideHelperExtendedSensorControl(new InstalledProbe()), TimeProvider.System, []);
    }

    private sealed class NoChanges : IHardwareChangeNotifier
    {
        public event EventHandler? Changed
        {
            add { }
            remove { }
        }
    }

    private sealed class InstalledProbe : IPawnIoInstallationProbe
    {
        public bool IsInstalled() => true;
    }

    /// <summary>テストの間だけ動かす <see cref="RemoteSensorService"/>。</summary>
    private sealed class RunningRemote : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellation = new();
        private readonly Task _run;

        public RunningRemote(PipeServerHarness harness)
        {
            Service = new RemoteSensorService(harness.Connector, TimeProvider.System, PipeServerHarness.FastRetry);
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

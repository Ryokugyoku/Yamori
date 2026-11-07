using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Registry;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Sensors;

public class SensorRegistryTests
{
    private static readonly SensorKey CpuLoad = SensorKey.Parse("cpu/0/load/total");
    private static readonly SensorKey CpuPackage = SensorKey.Parse("cpu/0/temperature/package");
    private static readonly SensorKey GpuLoad = SensorKey.Parse("gpu/3f9a12c4/load/max-engine");

    [Fact]
    public async Task CatalogMergesProvidersAndKeepsOneEntryPerKey()
    {
        var standard = new FakeSensorProvider("win-std").Declare(CpuLoad, 10).Declare(GpuLoad, 20);
        var pawnio = new FakeSensorProvider("pawnio").Declare(CpuLoad, 11, "from pawnio").Declare(CpuPackage, 55);
        var registry = CreateRegistry(standard, pawnio);

        var catalog = await registry.DiscoverAsync(CancellationToken.None);

        Assert.Equal([CpuLoad, CpuPackage, GpuLoad], catalog.Select(d => d.Key));
        // 既定の順（pawnio が先）で、説明も優先する Provider のものを使う。
        Assert.Equal("pawnio", catalog.Single(d => d.Key == CpuLoad).ProviderId);
        Assert.Equal("from pawnio", catalog.Single(d => d.Key == CpuLoad).DisplayName);
    }

    [Fact]
    public async Task ReadsFromThePreferredProviderAndRecordsTheSource()
    {
        var standard = new FakeSensorProvider("win-std").Declare(CpuLoad, 10);
        var pawnio = new FakeSensorProvider("pawnio").Declare(CpuLoad, 11);
        var registry = CreateRegistry(standard, pawnio);
        await registry.DiscoverAsync(CancellationToken.None);

        var reading = Assert.Single(await registry.ReadAllAsync(CancellationToken.None));

        Assert.Equal(SensorStatus.Ok, reading.Status);
        Assert.Equal(11, reading.Value);
        Assert.Equal("pawnio", reading.SourceProviderId);
        Assert.Empty(standard.ReadRequests);
    }

    [Fact]
    public async Task RuleOverridesTheDefaultOrder()
    {
        var standard = new FakeSensorProvider("win-std").Declare(CpuLoad, 10).Declare(GpuLoad, 20);
        var pawnio = new FakeSensorProvider("pawnio").Declare(CpuLoad, 11).Declare(GpuLoad, 21);
        var options = SensorPriorityOptions.Default with
        {
            Rules = [new SensorPriorityRule("gpu/*/*/*", ["win-std"])],
        };
        var registry = CreateRegistry(options, standard, pawnio);
        await registry.DiscoverAsync(CancellationToken.None);

        var readings = (await registry.ReadAllAsync(CancellationToken.None)).ToDictionary(r => r.Key);

        Assert.Equal("pawnio", readings[CpuLoad].SourceProviderId);
        Assert.Equal("win-std", readings[GpuLoad].SourceProviderId);
        Assert.Equal(20, readings[GpuLoad].Value);
    }

    [Theory]
    [InlineData(SensorStatus.TemporarilyUnavailable)]
    [InlineData(SensorStatus.Error)]
    public async Task FallsBackWhenThePreferredProviderCannotRead(SensorStatus status)
    {
        var standard = new FakeSensorProvider("win-std").Declare(CpuLoad, 10);
        var pawnio = new FakeSensorProvider("pawnio").Declare(CpuLoad, 11);
        pawnio.Statuses[CpuLoad] = status;
        var registry = CreateRegistry(standard, pawnio);
        await registry.DiscoverAsync(CancellationToken.None);

        var reading = Assert.Single(await registry.ReadAllAsync(CancellationToken.None));

        Assert.Equal(SensorStatus.Ok, reading.Status);
        Assert.Equal(10, reading.Value);
        Assert.Equal("win-std", reading.SourceProviderId);
    }

    [Fact]
    public async Task FallsBackWhenThePreferredProviderThrows()
    {
        // Helper が止まった場合など。
        var standard = new FakeSensorProvider("win-std").Declare(CpuLoad, 10);
        var pawnio = new FakeSensorProvider("pawnio").Declare(CpuLoad, 11).Declare(CpuPackage, 55);
        var registry = CreateRegistry(standard, pawnio);
        await registry.DiscoverAsync(CancellationToken.None);
        pawnio.ThrowOnRead = true;

        var readings = (await registry.ReadAllAsync(CancellationToken.None)).ToDictionary(r => r.Key);

        Assert.Equal(SensorStatus.Ok, readings[CpuLoad].Status);
        Assert.Equal("win-std", readings[CpuLoad].SourceProviderId);
        // 下位の Provider がないキーは、優先する Provider の失敗をそのまま返す。
        Assert.Equal(SensorStatus.Error, readings[CpuPackage].Status);
        Assert.Equal("pawnio", readings[CpuPackage].SourceProviderId);
        Assert.Null(readings[CpuPackage].Value);
    }

    [Fact]
    public async Task WhenNoProviderCanReadReturnsThePreferredProvidersResult()
    {
        var standard = new FakeSensorProvider("win-std").Declare(CpuLoad);
        var pawnio = new FakeSensorProvider("pawnio").Declare(CpuLoad);
        standard.Statuses[CpuLoad] = SensorStatus.Error;
        var registry = CreateRegistry(standard, pawnio);
        await registry.DiscoverAsync(CancellationToken.None);

        var reading = Assert.Single(await registry.ReadAllAsync(CancellationToken.None));

        Assert.Equal(SensorStatus.TemporarilyUnavailable, reading.Status);
        Assert.Equal("pawnio", reading.SourceProviderId);
    }

    [Fact]
    public async Task UnknownKeysAreUnsupportedAndAreNotSentToProviders()
    {
        var standard = new FakeSensorProvider("win-std").Declare(CpuLoad, 10);
        var registry = CreateRegistry(standard);
        await registry.DiscoverAsync(CancellationToken.None);

        var readings = await registry.ReadAsync([CpuLoad, GpuLoad], CancellationToken.None);

        Assert.Equal(SensorStatus.Ok, readings[0].Status);
        Assert.Equal(SensorStatus.Unsupported, readings[1].Status);
        Assert.Equal(string.Empty, readings[1].SourceProviderId);
        Assert.Equal([CpuLoad], Assert.Single(standard.ReadRequests));
    }

    [Fact]
    public async Task ProviderThatFailsToDiscoverContributesNothing()
    {
        var standard = new FakeSensorProvider("win-std").Declare(CpuLoad, 10);
        var pawnio = new FakeSensorProvider("pawnio") { ThrowOnDiscover = true }.Declare(CpuPackage, 55);
        var registry = CreateRegistry(standard, pawnio);

        var catalog = await registry.DiscoverAsync(CancellationToken.None);

        Assert.Equal([CpuLoad], catalog.Select(d => d.Key));
        Assert.Equal(["pawnio"], registry.FailedProviderIds);
    }

    [Fact]
    public async Task RegistryWithoutPawnIoWorksUnchanged()
    {
        var standard = new FakeSensorProvider("win-std").Declare(CpuLoad, 10);
        var registry = CreateRegistry(standard);
        await registry.DiscoverAsync(CancellationToken.None);

        var reading = Assert.Single(await registry.ReadAllAsync(CancellationToken.None));

        Assert.Equal("win-std", reading.SourceProviderId);
    }

    [Fact]
    public async Task DiscoverRaisesCatalogChanged()
    {
        var registry = CreateRegistry(new FakeSensorProvider("win-std").Declare(CpuLoad, 10));
        IReadOnlyList<SensorDescriptor>? raised = null;
        registry.CatalogChanged += (_, catalog) => raised = catalog;

        await registry.DiscoverAsync(CancellationToken.None);

        Assert.NotNull(raised);
        Assert.Single(raised);
        Assert.True(registry.TryGetDescriptor(CpuLoad, out _));
    }

    [Fact]
    public void DuplicateProviderIdsAreRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateRegistry(new FakeSensorProvider("win-std"), new FakeSensorProvider("win-std")));
    }

    private static SensorRegistry CreateRegistry(params FakeSensorProvider[] providers) =>
        CreateRegistry(SensorPriorityOptions.Default, providers);

    private static SensorRegistry CreateRegistry(SensorPriorityOptions options, params FakeSensorProvider[] providers) =>
        new(providers, new SensorPriorityPolicy(options), new ManualClock());
}

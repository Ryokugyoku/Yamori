using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Providers.PawnIo;

namespace Yamori.Core.Tests.Sensors;

public class ExtendedSensorTests
{
    [Theory]
    [InlineData(false, false, ExtendedSensorState.NotInstalled)]
    [InlineData(false, true, ExtendedSensorState.NotInstalled)]
    [InlineData(true, false, ExtendedSensorState.HelperRequired)]
    [InlineData(true, true, ExtendedSensorState.Active)]
    public void InitialStateDependsOnInstallationAndHelper(bool installed, bool canUse, ExtendedSensorState expected)
    {
        var resolver = new ExtendedSensorStatusResolver(new FixedProbe(installed));

        Assert.Equal(expected, resolver.ResolveInitial(canUse));
    }

    [Fact]
    public void FailedDiscoveryMakesActiveUnavailable()
    {
        Assert.Equal(ExtendedSensorState.Unavailable, ExtendedSensorStatusResolver.AfterDiscovery(ExtendedSensorState.Active, true));
        Assert.Equal(ExtendedSensorState.HelperRequired, ExtendedSensorStatusResolver.AfterDiscovery(ExtendedSensorState.HelperRequired, true));
    }

    [Fact]
    public async Task PawnIoStubDeclaresNothing()
    {
        var provider = new PawnIoProvider();

        Assert.Equal("pawnio", provider.ProviderId);
        Assert.Empty(await provider.DiscoverAsync(CancellationToken.None));
        var reading = Assert.Single(await provider.ReadAsync([WellKnownSensorKeys.CpuTotalLoad], CancellationToken.None));
        Assert.Equal(SensorStatus.Unsupported, reading.Status);
    }

    private sealed class FixedProbe(bool installed) : IPawnIoInstallationProbe
    {
        public bool IsInstalled() => installed;
    }
}

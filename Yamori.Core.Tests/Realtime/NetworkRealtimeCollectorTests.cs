using Yamori.Core.Hardware;
using Yamori.Core.Realtime;
using Yamori.Core.Realtime.Collectors;
using Yamori.Core.Realtime.Counters;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Realtime;

public class NetworkRealtimeCollectorTests
{
    private const string Ethernet = "Contoso[R] Ethernet [3] 2.5G";
    private const string Wifi = "Contoso Wi-Fi 7";
    private const string Virtual = "Hyper-V Virtual Ethernet Adapter";
    private const string Miniport = "WAN Miniport [IP]";

    private static FakeCounterSampler Sampler() =>
        new FakeCounterSampler()
            .Set(CounterPaths.NetworkBytesReceivedPerSecond, (Ethernet, 12_500_000), (Wifi, 1000), (Virtual, 500), (Miniport, 0))
            .Set(CounterPaths.NetworkBytesSentPerSecond, (Ethernet, 0), (Wifi, 250), (Virtual, 500), (Miniport, 0))
            .Set(CounterPaths.NetworkCurrentBandwidth, (Ethernet, 1_000_000_000), (Wifi, 0), (Virtual, 10_000_000_000), (Miniport, 0));

    [Fact]
    public void ShowsOnlyConnectedPhysicalAdaptersAndComputesUsage()
    {
        NetworkAdapterInfo[] adapters =
        [
            new("Contoso(R) Ethernet (3) 2.5G", NetworkConnectionKind.Ethernet, IsPhysical: true, IsConnected: true),
            new(Wifi, NetworkConnectionKind.WiFi, IsPhysical: true, IsConnected: false),
            new(Virtual, NetworkConnectionKind.Ethernet, IsPhysical: false, IsConnected: true),
        ];

        var networks = new NetworkRealtimeCollector(Sampler(), () => adapters).Collect();

        var network = Assert.Single(networks);
        Assert.Equal("Contoso(R) Ethernet (3) 2.5G", network.Name);
        Assert.Equal(NetworkConnectionKind.Ethernet, network.ConnectionKind);
        Assert.Equal(12_500_000, network.ReceivedBytesPerSecond);
        Assert.Equal(1_000_000_000, network.LinkSpeedBitsPerSecond);
        Assert.Equal(10, network.UsagePercent!.Value, 6);
    }

    [Fact]
    public void FallsBackToNameHintsWithoutAdapterList()
    {
        var networks = new NetworkRealtimeCollector(Sampler(), () => []).Collect();

        // リンク速度のない Wi-Fi と、仮想・WAN Miniport は出さない。
        var network = Assert.Single(networks);
        Assert.Equal(Ethernet, network.Name);
        Assert.Equal(NetworkConnectionKind.Unknown, network.ConnectionKind);
    }

    [Fact]
    public void ReturnsEmptyBeforeCountersAreReady()
    {
        Assert.Empty(new NetworkRealtimeCollector(new FakeCounterSampler(), () => []).Collect());
    }

    [Theory]
    [InlineData(1_250_000.0, 0.0, 100_000_000.0, 10.0)]
    [InlineData(10_000_000.0, 10_000_000.0, 100_000_000.0, 100.0)]
    [InlineData(1000.0, 0.0, 0.0, null)]
    [InlineData(null, null, 100_000_000.0, null)]
    public void ComputesUsageAgainstLinkSpeed(double? received, double? sent, double? link, double? expected)
    {
        Assert.Equal(expected, NetworkRealtimeCollector.ComputeUsage(received, sent, link));
    }

    [Fact]
    public void ConvertsDescriptionToCounterInstanceName()
    {
        Assert.Equal("Intel[R] Ethernet Controller [3] I225-V _2", NetworkCounterInstance.FromDescription("Intel(R) Ethernet Controller (3) I225-V #2"));
    }
}

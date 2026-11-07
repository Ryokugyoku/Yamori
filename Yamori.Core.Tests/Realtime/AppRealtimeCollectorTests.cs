using Yamori.Core.Realtime.Collectors;
using Yamori.Core.Realtime.Counters;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Realtime;

public class AppRealtimeCollectorTests
{
    private const string Luid = "luid_0x00000000_0x0000F69A_phys_0";

    [Fact]
    public void GroupsProcessV2InstancesByAppAndAttributesGpuByPid()
    {
        var sampler = new FakeCounterSampler()
            .Set(CounterPaths.ProcessV2ProcessorTime, ("chrome:10", 200), ("chrome:11", 100), ("game:20", 400), ("Idle:0", 700), ("_Total:0", 1600))
            .Set(CounterPaths.ProcessV2IoReadBytesPerSecond, ("chrome:10", 1000), ("game:20", 5000))
            .Set(CounterPaths.ProcessV2IoWriteBytesPerSecond, ("chrome:11", 3000))
            .Set(
                CounterPaths.GpuEngineUtilization,
                ($"pid_20_{Luid}_eng_0_engtype_3D", 60),
                ($"pid_20_{Luid}_eng_1_engtype_Copy", 5),
                ($"pid_10_{Luid}_eng_0_engtype_3D", 4),
                ($"pid_11_{Luid}_eng_2_engtype_VideoDecode", 7),
                ($"pid_99_{Luid}_eng_0_engtype_3D", 30));

        var apps = new AppRealtimeCollector(sampler, logicalProcessorCount: 16).Collect();

        Assert.Equal(["game", "chrome"], apps.TopByCpu.Select(a => a.Name));
        var chrome = apps.TopByCpu[1];
        Assert.Equal((2, 18.75), (chrome.ProcessCount, chrome.CpuPercent));
        Assert.Equal(11, chrome.GpuPercent);
        Assert.Equal(25, apps.TopByCpu[0].CpuPercent);

        Assert.Equal([("game", 60.0), ("chrome", 11.0)], apps.TopByGpu.Select(a => (a.Name, a.GpuPercent)));
        Assert.Equal([("game", 5000.0), ("chrome", 4000.0)], apps.TopByIo.Select(a => (a.Name, a.IoBytesPerSecond)));
    }

    [Fact]
    public void FallsBackToLegacyProcessCountersWithDuplicateNames()
    {
        var sampler = new FakeCounterSampler()
            .Set(CounterPaths.ProcessId, ("svchost", 100), ("svchost#1", 101), ("Idle", 0))
            .Set(CounterPaths.ProcessProcessorTime, ("svchost", 50), ("svchost#1", 30), ("Idle", 300))
            .Set(CounterPaths.ProcessIoWriteBytesPerSecond, ("svchost#1", 2048));

        var apps = new AppRealtimeCollector(sampler, logicalProcessorCount: 4).Collect();

        var app = Assert.Single(apps.TopByCpu);
        Assert.Equal(("svchost", 2, 20.0), (app.Name, app.ProcessCount, app.CpuPercent));
        Assert.Equal(2048, Assert.Single(apps.TopByIo).WriteBytesPerSecond);
        Assert.Empty(apps.TopByGpu);
    }

    [Fact]
    public void ReturnsEmptyWhenNoProcessCountersAreReadable()
    {
        var apps = new AppRealtimeCollector(new FakeCounterSampler(), 8).Collect();

        Assert.Empty(apps.TopByCpu);
        Assert.Empty(apps.TopByGpu);
        Assert.Empty(apps.TopByIo);
    }

    [Theory]
    [InlineData("chrome:1234", "chrome", 1234)]
    [InlineData("my:app:42", "my:app", 42)]
    public void ParsesProcessV2InstanceNames(string instance, string name, int processId)
    {
        Assert.Equal((name, processId), ProcessCounterInstance.TryParseNameAndId(instance));
    }

    [Theory]
    [InlineData("chrome")]
    [InlineData(":12")]
    [InlineData("chrome:abc")]
    [InlineData(null)]
    public void RejectsOtherProcessV2InstanceNames(string? instance)
    {
        Assert.Null(ProcessCounterInstance.TryParseNameAndId(instance));
    }

    [Theory]
    [InlineData("chrome#12", "chrome")]
    [InlineData("chrome", "chrome")]
    [InlineData("C#", "C#")]
    [InlineData("app#beta", "app#beta")]
    public void StripsDuplicateIndexFromLegacyNames(string instance, string expected)
    {
        Assert.Equal(expected, ProcessCounterInstance.StripDuplicateIndex(instance));
    }

    [Theory]
    [InlineData("pid_1234_luid_0x0_0x1_phys_0_eng_0_engtype_3D", 1234)]
    [InlineData("luid_0x0_0x1_phys_0", null)]
    [InlineData("pid_x_luid", null)]
    public void ParsesGpuProcessIds(string instance, int? expected)
    {
        Assert.Equal(expected, ProcessCounterInstance.TryParseGpuProcessId(instance));
    }
}

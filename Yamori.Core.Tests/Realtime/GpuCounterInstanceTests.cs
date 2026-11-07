using Yamori.Core.Realtime.Counters;

namespace Yamori.Core.Tests.Realtime;

public class GpuCounterInstanceTests
{
    [Fact]
    public void ParsesEngineInstances()
    {
        var instance = GpuCounterInstance.TryParse("pid_1234_luid_0x00000000_0x0000F69A_phys_0_eng_3_engtype_VideoDecode");

        Assert.Equal(new GpuCounterInstance(0xF69A, 3, "VideoDecode"), instance);
    }

    [Fact]
    public void ParsesMemoryInstancesWithoutEngine()
    {
        var instance = GpuCounterInstance.TryParse("luid_0x00000001_0x00011698_phys_0");

        Assert.Equal(new GpuCounterInstance(0x1_0001_1698, null, null), instance);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("_Total")]
    [InlineData("pid_1_luid_0xZZ_0x01_phys_0")]
    public void ReturnsNullForOtherNames(string? name)
    {
        Assert.Null(GpuCounterInstance.TryParse(name));
    }
}

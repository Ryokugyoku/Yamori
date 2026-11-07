using Yamori.Core.Sensors;

namespace Yamori.Core.Tests.Sensors;

public class HardwareIdsTests
{
    [Fact]
    public void FromPersistentIdIsEightLowercaseHexDigits()
    {
        var id = HardwareIds.FromPersistentId(@"PCI\VEN_10DE&DEV_2786&SUBSYS_00000000&REV_A1\4&1234");

        Assert.Matches("^[0-9a-f]{8}$", id);
        Assert.True(SensorKeyTokens.IsNormalizedSegment(id));
    }

    [Fact]
    public void FromPersistentIdIsDeterministicAndIgnoresCaseAndSurroundingSpace()
    {
        Assert.Equal(
            HardwareIds.FromPersistentId("serial:S6B0NL0W123456"),
            HardwareIds.FromPersistentId("  SERIAL:s6b0nl0w123456 "));
    }

    [Fact]
    public void FromPersistentIdDiffersForDifferentDevices()
    {
        Assert.NotEqual(HardwareIds.FromPersistentId("pci:01:00.0"), HardwareIds.FromPersistentId("pci:02:00.0"));
    }

    [Fact]
    public void FromPersistentIdDoesNotExposeTheIdentifier()
    {
        Assert.DoesNotContain("123456", HardwareIds.FromPersistentId("serial:123456"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromPersistentIdRejectsBlank(string value)
    {
        Assert.Throws<ArgumentException>(() => HardwareIds.FromPersistentId(value));
    }

    [Fact]
    public void FromSocketUsesTheSocketNumber()
    {
        Assert.Equal("0", HardwareIds.FromSocket(0));
        Assert.Equal("1", HardwareIds.FromSocket(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => HardwareIds.FromSocket(-1));
    }
}

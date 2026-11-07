using Yamori.Core.Sensors;

namespace Yamori.Core.Tests.Sensors;

public class SensorKeyTests
{
    [Fact]
    public void CreateFormatsAsFourSegments()
    {
        var key = SensorKey.Create(HardwareKind.Cpu, "0", SensorType.Temperature, "package");

        Assert.Equal("cpu/0/temperature/package", key.ToString());
    }

    [Theory]
    [InlineData("Thread #3", "thread-3")]
    [InlineData("  Video Decode  ", "video-decode")]
    [InlineData("Compute_0", "compute_0")]
    [InlineData("a//b", "a-b")]
    [InlineData("--x--", "x")]
    public void CreateNormalizesSegments(string name, string expected)
    {
        var key = SensorKey.Create(HardwareKind.Gpu, "ABC", SensorType.Load, name);

        Assert.Equal(expected, key.Name);
        Assert.Equal("abc", key.HardwareId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("///")]
    [InlineData("*")]
    public void CreateRejectsSegmentsWithoutUsableCharacters(string name)
    {
        Assert.Throws<ArgumentException>(() => SensorKey.Create(HardwareKind.Cpu, "0", SensorType.Load, name));
    }

    [Fact]
    public void ParseRoundTripsEveryKindAndType()
    {
        foreach (var kind in Enum.GetValues<HardwareKind>())
        {
            foreach (var type in Enum.GetValues<SensorType>())
            {
                var key = SensorKey.Create(kind, "3f9a12c4", type, "value");
                Assert.Equal(key, SensorKey.Parse(key.ToString()));
            }
        }
    }

    [Theory]
    [InlineData("cpu/0/temperature")]
    [InlineData("cpu/0/temperature/package/extra")]
    [InlineData("gpgpu/0/load/total")]
    [InlineData("cpu/0/heat/package")]
    [InlineData("cpu/0/load/Total")]
    [InlineData("cpu/*/load/total")]
    [InlineData("cpu//load/total")]
    [InlineData("CPU/0/load/total")]
    public void TryParseRejectsNonCanonicalText(string text)
    {
        Assert.False(SensorKey.TryParse(text, out _));
        Assert.Throws<FormatException>(() => SensorKey.Parse(text));
    }

    [Fact]
    public void KeysWithSameSegmentsAreEqual()
    {
        var a = SensorKey.Create(HardwareKind.Memory, "0", SensorType.Load, "physical");
        var b = SensorKey.Parse("memory/0/load/physical");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(WellKnownSensorKeys.MemoryPhysicalLoad, a);
    }

    [Fact]
    public void AcpiZoneAndPackageTemperatureAreDifferentKeys()
    {
        var package = SensorKey.Create(HardwareKind.Cpu, "0", SensorType.Temperature, "package");
        var zone = SensorKey.Create(HardwareKind.ThermalZone, HardwareIds.FromPersistentId(@"acpi-tz:\_TZ.TZ00"), SensorType.Temperature, "zone");

        Assert.NotEqual(package, zone);
    }
}

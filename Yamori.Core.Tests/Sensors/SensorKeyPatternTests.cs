using Yamori.Core.Sensors;

namespace Yamori.Core.Tests.Sensors;

public class SensorKeyPatternTests
{
    [Theory]
    [InlineData("gpu/*/load/max-engine", "gpu/3f9a12c4/load/max-engine", true)]
    [InlineData("gpu/*/load/max-engine", "gpu/3f9a12c4/load/engine-3d", false)]
    [InlineData("*/*/temperature/*", "cpu/0/temperature/package", true)]
    [InlineData("*/*/temperature/*", "cpu/0/load/total", false)]
    [InlineData("cpu/0/load/total", "cpu/0/load/total", true)]
    [InlineData("cpu/1/load/total", "cpu/0/load/total", false)]
    public void MatchesSegmentBySegment(string pattern, string key, bool expected)
    {
        Assert.Equal(expected, SensorKeyPattern.Parse(pattern).Matches(SensorKey.Parse(key)));
    }

    [Theory]
    [InlineData("gpu/*/load")]
    [InlineData("gpgpu/*/load/total")]
    [InlineData("gpu/*/heat/total")]
    [InlineData("gpu/ABC/load/total")]
    public void ParseRejectsInvalidPatterns(string pattern)
    {
        Assert.Throws<FormatException>(() => SensorKeyPattern.Parse(pattern));
    }
}

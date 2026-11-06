using Yamori.Core.Analysis;

namespace Yamori.Core.Tests.Analysis;

public class LoadAnalyzerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 6, 9, 0, 0, TimeSpan.FromHours(9));

    private static UsageSample At(int minutes, double usage) => new(T0.AddMinutes(minutes), usage);

    [Fact]
    public void ComputesShareOfTimeAboveEachThreshold()
    {
        // 0-1分: 90%, 1-2分: 60%, 2-3分: 30%, 3-4分: 85%（最後のサンプルは終点）
        var samples = new[] { At(0, 90), At(1, 60), At(2, 30), At(3, 85), At(4, 10) };

        var result = LoadAnalyzer.ComputeExposure(samples, [50, 80]);

        Assert.Equal(TimeSpan.FromMinutes(4), result[0].TotalObserved);
        Assert.Equal(TimeSpan.FromMinutes(3), result[0].TimeAbove);
        Assert.Equal(0.75, result[0].Ratio, precision: 10);
        Assert.Equal(TimeSpan.FromMinutes(2), result[1].TimeAbove);
        Assert.Equal(0.5, result[1].Ratio, precision: 10);
    }

    [Fact]
    public void IgnoresGapsLongerThanMaxGap()
    {
        // 1分のあと、スリープで2時間空いた
        var samples = new[] { At(0, 95), At(1, 95), At(121, 95) };

        var result = LoadAnalyzer.ComputeExposure(samples, [80]);

        Assert.Equal(TimeSpan.FromMinutes(1), result[0].TotalObserved);
        Assert.Equal(1.0, result[0].Ratio, precision: 10);
    }

    [Fact]
    public void SortsUnorderedSamples()
    {
        var samples = new[] { At(2, 0), At(0, 100), At(1, 0) };

        var result = LoadAnalyzer.ComputeExposure(samples, [80]);

        Assert.Equal(0.5, result[0].Ratio, precision: 10);
    }

    [Fact]
    public void ValueEqualToThresholdDoesNotCountAsAbove()
    {
        var samples = new[] { At(0, 80), At(1, 80) };

        var result = LoadAnalyzer.ComputeExposure(samples, [80]);

        Assert.Equal(TimeSpan.Zero, result[0].TimeAbove);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ReturnsZeroRatioWhenNothingWasObserved(int sampleCount)
    {
        var samples = Enumerable.Range(0, sampleCount).Select(i => At(i, 100));

        var result = LoadAnalyzer.ComputeExposure(samples, [50]);

        Assert.Equal(TimeSpan.Zero, result[0].TotalObserved);
        Assert.Equal(0, result[0].Ratio);
    }
}

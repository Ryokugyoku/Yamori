using Yamori.Core.AppUsage;
using Yamori.Core.AppUsage.Recording;

namespace Yamori.Core.Tests.AppUsage;

public class AppCpuMinuteAccumulatorTests
{
    private static readonly DateTimeOffset Minute = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);

    private static AppCpuSample Sample(int second, double total, params (string Name, double Percent)[] apps) =>
        new(Minute.AddSeconds(second), total, apps.Select(a => new AppCpuShare(a.Name, a.Percent)).ToArray());

    [Fact]
    public void AveragesSamplesWithinTheSameMinuteAndCountsMissingAppsAsZero()
    {
        var accumulator = new AppCpuMinuteAccumulator();

        Assert.Null(accumulator.Add(Sample(10, 50, ("chrome", 30), ("code", 10))));
        Assert.Null(accumulator.Add(Sample(20, 70, ("Chrome", 50))));
        var minute = accumulator.Add(Sample(65, 10, ("chrome", 1)));

        Assert.NotNull(minute);
        Assert.Equal(Minute, minute.MinuteStartUtc);
        Assert.Equal(2, minute.SampleCount);
        Assert.Equal(60, minute.TotalPercent, 6);
        Assert.Equal([("chrome", 40d), ("code", 5d)], minute.Apps.Select(a => (a.Name, a.CpuPercent)));
    }

    [Fact]
    public void KeepsOnlyTheTopAppsAboveTheMinimum()
    {
        var accumulator = new AppCpuMinuteAccumulator();
        var apps = Enumerable.Range(1, 15).Select(i => ($"app{i:D2}", (double)i)).Append(("tiny", 0.05)).ToArray();
        accumulator.Add(Sample(0, 90, apps));

        var minute = accumulator.Flush();

        Assert.NotNull(minute);
        Assert.Equal(AppCpuMinuteAccumulator.MaxAppsPerMinute, minute.Apps.Count);
        Assert.Equal("app15", minute.Apps[0].Name);
        Assert.DoesNotContain(minute.Apps, a => a.Name == "tiny");
    }

    [Fact]
    public void FlushReturnsNullWhenNothingWasAddedAndResets()
    {
        var accumulator = new AppCpuMinuteAccumulator();
        Assert.Null(accumulator.Flush());

        accumulator.Add(Sample(0, 20, ("chrome", 5)));
        Assert.NotNull(accumulator.Flush());
        Assert.Null(accumulator.Flush());
    }

    [Fact]
    public void ClockGoingBackClosesTheCurrentMinute()
    {
        var accumulator = new AppCpuMinuteAccumulator();
        accumulator.Add(Sample(70, 20));

        var closed = accumulator.Add(Sample(10, 40));

        Assert.NotNull(closed);
        Assert.Equal(Minute.AddMinutes(1), closed.MinuteStartUtc);
        Assert.Equal(Minute, accumulator.Flush()!.MinuteStartUtc);
    }
}

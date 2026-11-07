using System.Globalization;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Display;

namespace Yamori.Core.Tests.Sensors;

public class DisplayTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 7, 0, 0, 0, TimeSpan.Zero);
    private static readonly SensorKey CpuLoad = SensorKey.Parse("cpu/0/load/total");

    [Fact]
    public void CardUsesFirstAvailableCandidate()
    {
        var card = SummaryCardDefinitions.Default.Single(c => c.Id == "CpuTemperature");
        var zone = SensorKey.Parse("thermal-zone/cb69d6d5/temperature/zone");
        var package = SensorKey.Parse("cpu/0/temperature/package");

        Assert.Equal(zone, SummaryCardResolver.Resolve(card, [CpuLoad, zone]));
        Assert.Equal(package, SummaryCardResolver.Resolve(card, [zone, package]));
    }

    [Fact]
    public void CardWithoutCandidatesIsHidden()
    {
        var card = SummaryCardDefinitions.Default.Single(c => c.Id == "GpuLoad");

        Assert.Null(SummaryCardResolver.Resolve(card, [CpuLoad]));
    }

    [Fact]
    public void WildcardCandidatePicksTheFirstKeyInStableOrder()
    {
        var card = SummaryCardDefinitions.Default.Single(c => c.Id == "GpuLoad");
        var b = SensorKey.Parse("gpu/bbbbbbbb/load/max-engine");
        var a = SensorKey.Parse("gpu/aaaaaaaa/load/max-engine");

        Assert.Equal(a, SummaryCardResolver.Resolve(card, [b, a]));
    }

    [Fact]
    public void VisibilityHidesOnlyAfterRepeatedFailures()
    {
        var tracker = new SensorVisibilityTracker(hideAfterFailures: 3, showAfterSuccesses: 2);
        tracker.SetCatalog([CpuLoad]);
        Assert.Equal(SensorDisplayState.Unavailable, tracker.GetState(CpuLoad));

        tracker.Apply(Ok(10));
        Assert.Equal(SensorDisplayState.Value, tracker.GetState(CpuLoad));

        tracker.Apply(Failed(SensorStatus.TemporarilyUnavailable));
        tracker.Apply(Failed(SensorStatus.Error));
        Assert.Equal(SensorDisplayState.Unavailable, tracker.GetState(CpuLoad));

        tracker.Apply(Failed(SensorStatus.TemporarilyUnavailable));
        Assert.Equal(SensorDisplayState.Hidden, tracker.GetState(CpuLoad));

        tracker.Apply(Ok(11));
        Assert.Equal(SensorDisplayState.Hidden, tracker.GetState(CpuLoad));
        tracker.Apply(Ok(12));
        Assert.Equal(SensorDisplayState.Value, tracker.GetState(CpuLoad));
    }

    [Fact]
    public void AlternatingFailuresDoNotFlicker()
    {
        var tracker = new SensorVisibilityTracker();
        tracker.SetCatalog([CpuLoad]);

        for (var i = 0; i < 10; i++)
        {
            tracker.Apply(i % 2 == 0 ? Failed(SensorStatus.TemporarilyUnavailable) : Ok(i));
            Assert.NotEqual(SensorDisplayState.Hidden, tracker.GetState(CpuLoad));
        }
    }

    [Fact]
    public void UnsupportedAndUnknownKeysAreHidden()
    {
        var tracker = new SensorVisibilityTracker();
        tracker.SetCatalog([CpuLoad]);

        tracker.Apply(Failed(SensorStatus.Unsupported));

        Assert.Equal(SensorDisplayState.Hidden, tracker.GetState(CpuLoad));
        Assert.Equal(SensorDisplayState.Hidden, tracker.GetState(SensorKey.Parse("gpu/x/load/core")));

        tracker.SetCatalog([]);
        Assert.Empty(tracker.VisibleKeys());
    }

    [Fact]
    public void TreeGroupsByHardwareThenGroupAndSortsNaturally()
    {
        SensorDescriptor D(string key, string hardware, string group) =>
            new(SensorKey.Parse(key), SensorUnit.Percent, key, hardware, group, "win-std");
        SensorDescriptor[] catalog =
        [
            D("gpu/aaaaaaaa/load/max-engine", "GPU", "load"),
            D("cpu/0/load/thread-10", "CPU", "threads"),
            D("cpu/0/load/thread-2", "CPU", "threads"),
            D("cpu/0/clock/base", "CPU", "clock"),
            D("cpu/0/load/total", "CPU", "load"),
            D("memory/0/load/physical", "Memory", "load"),
        ];

        var tree = SensorTreeBuilder.Build(catalog, k => k != SensorKey.Parse("memory/0/load/physical"));

        Assert.Equal(["cpu/0", "gpu/aaaaaaaa"], tree.Select(h => h.Key));
        var cpu = tree[0];
        Assert.Equal(["load", "threads", "clock"], cpu.Groups.Select(g => g.Group));
        Assert.Equal(["thread-2", "thread-10"], cpu.Groups[1].Sensors.Select(s => s.Key.Name));
    }

    [Theory]
    [InlineData("thread-2", "thread-10", -1)]
    [InlineData("thread-10", "thread-10", 0)]
    [InlineData("a", "B", -1)]
    [InlineData("disk 02", "disk 1", 1)]
    public void NaturalComparerComparesNumbersAsNumbers(string x, string y, int expected)
    {
        Assert.Equal(expected, Math.Sign(NaturalStringComparer.Instance.Compare(x, y)));
    }

    [Theory]
    [InlineData(4_500_000_000d, SensorUnit.Hertz, "4.50 GHz")]
    [InlineData(800_000_000d, SensorUnit.Hertz, "800 MHz")]
    [InlineData(39.44, SensorUnit.Percent, "39.4%")]
    [InlineData(61.25, SensorUnit.Celsius, "61.3 °C")]
    [InlineData(1.2345, SensorUnit.Volt, "1.235 V")]
    [InlineData(0.002, SensorUnit.Second, "2 ms")]
    [InlineData(93_784d, SensorUnit.Second, "1d 02:03:04")]
    [InlineData(1_536d, SensorUnit.BytePerSecond, "1.5 KB/s")]
    [InlineData(1450d, SensorUnit.Rpm, "1,450 RPM")]
    public void FormatterNormalizesUnits(double value, SensorUnit unit, string expected)
    {
        Assert.Equal(expected, SensorValueFormatter.Format(value, unit, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FormatterShowsDashWithoutValue()
    {
        Assert.Equal("—", SensorValueFormatter.Format(null, SensorUnit.Celsius, CultureInfo.InvariantCulture));
        Assert.Equal(100, SensorValueFormatter.ToPercent(140, SensorUnit.Percent));
        Assert.Null(SensorValueFormatter.ToPercent(50, SensorUnit.Celsius));
    }

    private static SensorReading Ok(double value) => SensorReading.Ok(CpuLoad, value, Now, "win-std");

    private static SensorReading Failed(SensorStatus status) => SensorReading.Failed(CpuLoad, status, Now, "win-std");
}

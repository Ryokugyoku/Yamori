using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Display;

namespace Yamori.Core.Tests.Sensors;

public class RealtimeLayoutTests
{
    [Fact]
    public void HighlightsFollowSlotsAndPreferEarlierCandidates()
    {
        var hardware = Hardware(
            HardwareKind.Cpu,
            Group("load", D("cpu/0/load/dpc"), D("cpu/0/load/total")),
            Group("clock", D("cpu/0/clock/average-estimated"), D("cpu/0/clock/average-effective"), D("cpu/0/clock/base")),
            Group("power", D("cpu/0/power/package")));

        var highlights = SensorHighlightSelector.Select(hardware);

        Assert.Equal(["load/total", "clock/average-effective", "power/package"], highlights.Select(Token));
    }

    [Fact]
    public void HighlightsFallBackToFirstSensorOfSmallGroups()
    {
        var threads = Enumerable.Range(0, SensorPanelBuilder.DenseThreshold).Select(i => D($"motherboard/0/fan/fan-{i}")).ToArray();
        var hardware = Hardware(
            HardwareKind.Motherboard,
            Group("fans", threads),
            Group("voltage", D("motherboard/0/voltage/vcore"), D("motherboard/0/voltage/v12")));

        var highlights = SensorHighlightSelector.Select(hardware);

        Assert.Equal(["voltage/vcore"], highlights.Select(Token));
    }

    [Fact]
    public void MixedGroupIsSplitByTypeAndDenseWhenLarge()
    {
        var sensors = Enumerable.Range(0, 8)
            .SelectMany(i => new[]
            {
                D($"cpu/0/load/thread-{i}", $"Thread #{i}"),
                D($"cpu/0/clock/thread-{i}-estimated", $"Thread #{i} Clock (estimated)"),
            })
            .ToArray();

        var panels = SensorPanelBuilder.Build(new SensorTreeGroup("cpu/0/threads", "threads", sensors));

        Assert.Equal(["cpu/0/threads/load", "cpu/0/threads/clock"], panels.Select(p => p.Key));
        Assert.All(panels, p => Assert.True(p.IsDense));
        Assert.Equal(SensorType.Clock, panels[1].SplitType);
        Assert.Equal("Thread #3", panels[1].Labels[3]);
        Assert.Equal("Clock (estimated)", panels[1].CommonSuffix);
        Assert.Equal(string.Empty, panels[0].CommonSuffix);
    }

    [Fact]
    public void SingleTypeGroupIsKeptAsOnePanel()
    {
        var panels = SensorPanelBuilder.Build(new SensorTreeGroup("cpu/0/load", "load", [D("cpu/0/load/total"), D("cpu/0/load/user")]));

        var panel = Assert.Single(panels);
        Assert.Equal("cpu/0/load", panel.Key);
        Assert.Null(panel.SplitType);
        Assert.False(panel.IsDense);
    }

    [Theory]
    [InlineData(new[] { "Package (RAPL)", "Cores (RAPL)" }, new[] { "Package", "Cores" }, "(RAPL)")]
    [InlineData(new[] { "Thread #1", "Thread #11" }, new[] { "Thread #1", "Thread #11" }, "")]
    [InlineData(new[] { "CPU Total", "User" }, new[] { "CPU Total", "User" }, "")]
    [InlineData(new[] { "Read", "Read" }, new[] { "Read", "Read" }, "")]
    [InlineData(new[] { "Only one" }, new[] { "Only one" }, "")]
    public void CommonSuffixIsStrippedAtWordBoundary(string[] labels, string[] expected, string suffix)
    {
        var (actual, actualSuffix) = SensorLabelShortener.StripCommonSuffix(labels);

        Assert.Equal(expected, actual);
        Assert.Equal(suffix, actualSuffix);
    }

    private static string Token(SensorDescriptor d) => SensorKeyTokens.ToToken(d.SensorType) + "/" + d.Key.Name;

    private static SensorDescriptor D(string key, string? name = null) =>
        new(SensorKey.Parse(key), SensorUnit.Percent, name ?? key, "HW", "g", "win-std");

    private static SensorTreeGroup Group(string name, params SensorDescriptor[] sensors) => new("hw/" + name, name, sensors);

    private static SensorTreeHardware Hardware(HardwareKind kind, params SensorTreeGroup[] groups) => new("hw", kind, "HW", groups);
}

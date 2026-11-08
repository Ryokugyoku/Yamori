using Yamori.Core.AppUsage.Analysis;
using Yamori.Core.AppUsage.Storage;

namespace Yamori.Core.Tests.AppUsage;

public class AppCpuUsageSummarizerTests
{
    [Fact]
    public void ComputesAveragesOverObservedAndHighLoadMinutes()
    {
        var totals = new AppCpuTotals(100, 10, [new AppCpuTotal("chrome", 500, 300), new AppCpuTotal("code", 200, 0)]);

        var summary = AppCpuUsageSummarizer.Summarize(totals, 80, 5);

        Assert.Equal(TimeSpan.FromMinutes(100), summary.Observed);
        Assert.Equal(TimeSpan.FromMinutes(10), summary.HighLoad);
        Assert.Equal(80, summary.HighLoadThresholdPercent);
        Assert.Equal([new AppCpuUsage("chrome", 5, 30), new AppCpuUsage("code", 2, 0)], summary.Apps);
    }

    [Fact]
    public void KeepsAppsThatRankHighOnlyDuringHighLoad()
    {
        // render は平均では 3 位だが、高負荷の時間では 1 位。
        var totals = new AppCpuTotals(100, 5, [
            new AppCpuTotal("chrome", 900, 10),
            new AppCpuTotal("code", 800, 5),
            new AppCpuTotal("render", 300, 290),
        ]);

        var summary = AppCpuUsageSummarizer.Summarize(totals, 80, 1);

        Assert.Equal(["chrome", "render"], summary.Apps.Select(a => a.Name));
        Assert.Equal(["render", "chrome"], summary.TopHighLoadApps(2, 1));
    }

    [Fact]
    public void NoObservedMinutesMeansEmpty()
    {
        Assert.Same(AppCpuUsageSummary.Empty, AppCpuUsageSummarizer.Summarize(AppCpuTotals.Empty, 80, 5));
        Assert.Empty(AppCpuUsageSummary.Empty.TopHighLoadApps(2, 1));
    }

    [Fact]
    public void TopHighLoadAppsSkipsAppsBelowTheMinimum()
    {
        var summary = new AppCpuUsageSummary(TimeSpan.FromHours(1), TimeSpan.FromMinutes(5), 80, [
            new AppCpuUsage("chrome", 10, 45),
            new AppCpuUsage("idle-ish", 1, 0.5),
        ]);

        Assert.Equal(["chrome"], summary.TopHighLoadApps(2, 1));
    }
}

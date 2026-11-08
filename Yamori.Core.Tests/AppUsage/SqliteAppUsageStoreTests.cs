using Yamori.Core.AppUsage;
using Yamori.Core.AppUsage.Storage;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.AppUsage;

public class SqliteAppUsageStoreTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);

    private static AppCpuMinute Minute(int offset, double total, int count, params (string Name, double Percent)[] apps) =>
        new(Start.AddMinutes(offset), total, count, apps.Select(a => new AppCpuShare(a.Name, a.Percent)).ToArray());

    [Fact]
    public void TotalsSplitHighLoadMinutes()
    {
        using var store = SqliteAppUsageStore.Open(":memory:");
        store.WriteMinute(Minute(0, 90, 6, ("chrome", 60), ("teams", 20)));
        store.WriteMinute(Minute(1, 30, 6, ("chrome", 10)));
        store.WriteMinute(Minute(2, 85, 6, ("teams", 70)));

        var totals = store.ReadTotals(Start, Start.AddMinutes(3), 80);

        Assert.Equal(3, totals.ObservedMinutes);
        Assert.Equal(2, totals.HighLoadMinutes);
        var chrome = totals.Apps.Single(a => a.Name == "chrome");
        Assert.Equal(70, chrome.CpuPercentSum, 6);
        Assert.Equal(60, chrome.HighLoadCpuPercentSum, 6);
        var teams = totals.Apps.Single(a => a.Name == "teams");
        Assert.Equal(90, teams.CpuPercentSum, 6);
        Assert.Equal(90, teams.HighLoadCpuPercentSum, 6);
    }

    [Fact]
    public void RangeEndIsExclusiveAndEmptyRangeHasNoData()
    {
        using var store = SqliteAppUsageStore.Open(":memory:");
        store.WriteMinute(Minute(0, 50, 6, ("chrome", 10)));
        store.WriteMinute(Minute(1, 50, 6, ("chrome", 10)));

        Assert.Equal(1, store.ReadTotals(Start, Start.AddMinutes(1), 80).ObservedMinutes);
        Assert.Same(AppCpuTotals.Empty, store.ReadTotals(Start.AddHours(1), Start.AddHours(2), 80));
    }

    [Fact]
    public void WritingTheSameMinuteAgainMergesByWeight()
    {
        using var store = SqliteAppUsageStore.Open(":memory:");
        store.WriteMinute(Minute(0, 40, 3, ("chrome", 20)));
        store.WriteMinute(Minute(0, 80, 1, ("Chrome", 40), ("code", 8)));

        var totals = store.ReadTotals(Start, Start.AddMinutes(1), 10);

        Assert.Equal(1, totals.ObservedMinutes);
        Assert.Equal(25, totals.Apps.Single(a => a.Name.Equals("chrome", StringComparison.OrdinalIgnoreCase)).CpuPercentSum, 6);
        Assert.Equal(2, totals.Apps.Single(a => a.Name == "code").CpuPercentSum, 6);
    }

    [Fact]
    public void DeleteBeforeRemovesOldMinutesAndUnusedNames()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "app-usage.db");
        using (var store = SqliteAppUsageStore.Open(path))
        {
            store.WriteMinute(Minute(0, 90, 6, ("old-app", 50)));
            store.WriteMinute(Minute(10, 90, 6, ("chrome", 50)));

            store.DeleteBefore(Start.AddMinutes(5));
            store.WriteMinute(Minute(11, 90, 6, ("old-app", 5)));
        }

        using var reopened = SqliteAppUsageStore.Open(path);
        var totals = reopened.ReadTotals(Start, Start.AddHours(1), 80);

        Assert.Equal(2, totals.ObservedMinutes);
        Assert.Equal(5, totals.Apps.Single(a => a.Name == "old-app").CpuPercentSum, 6);
        Assert.Equal(50, totals.Apps.Single(a => a.Name == "chrome").CpuPercentSum, 6);
    }
}

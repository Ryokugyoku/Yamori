using Yamori.Core.Analysis;
using Yamori.Core.Formatting;
using Yamori.Core.Hardware;
using Yamori.Core.Processes;
using Yamori.Core.Recommendations;

namespace Yamori.Core.Tests.Recommendations;

public class NextPcAdvisorTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 6, 9, 0, 0, TimeSpan.FromHours(9));

    private static readonly HardwareInventory Inventory = HardwareInventory.Empty with
    {
        Cpu = new CpuInfo("CPU", 4, 8, 3000),
        Memory = new MemoryInfo(16UL << 30, 2, 4800),
    };

    private static readonly ProcessUsage[] TopApps = [new("chrome", 20, 6UL << 30), new("Teams", 5, 2UL << 30), new("Code", 3, 1UL << 30)];

    /// <summary>
    /// 1 分おきのサンプルを作る。先頭の <paramref name="highMinutes"/> 分は 90%、残りは 20%。
    /// </summary>
    private static UsageSample[] Samples(int totalMinutes, int highMinutes) =>
        Enumerable.Range(0, totalMinutes + 1)
            .Select(i => new UsageSample(T0.AddMinutes(i), i < highMinutes ? 90 : 20))
            .ToArray();

    private static HardwareRecommendation EvaluateMemory(UsageSample[] samples) =>
        NextPcAdvisor.CreateDefault()
            .Advise(Inventory, new Dictionary<HardwareComponent, IReadOnlyList<UsageSample>> { [HardwareComponent.Memory] = samples }, TopApps)
            .Single(r => r.Component == HardwareComponent.Memory);

    [Fact]
    public void RecommendsNextMemoryTierWhenHighLoadIsLong()
    {
        // 100 分のうち 35 分が 80% 超 → 35%
        var result = EvaluateMemory(Samples(100, 35));

        Assert.Equal(RecommendationLevel.Insufficient, result.Level);
        Assert.Equal(16, result.CurrentAmount);
        Assert.Equal(32, result.RecommendedAmount);
        Assert.Equal(["chrome", "Teams"], result.MainCauses);
        Assert.Equal(0.35, result.HighLoad.Ratio, precision: 10);
    }

    [Fact]
    public void BorderlineAlsoSuggestsNextTier()
    {
        var result = EvaluateMemory(Samples(100, 10));

        Assert.Equal(RecommendationLevel.Borderline, result.Level);
        Assert.Equal(32, result.RecommendedAmount);
    }

    [Fact]
    public void KeepsCurrentTierWhenSufficient()
    {
        var result = EvaluateMemory(Samples(100, 1));

        Assert.Equal(RecommendationLevel.Sufficient, result.Level);
        Assert.Equal(16, result.RecommendedAmount);
        Assert.Empty(result.MainCauses);
    }

    [Fact]
    public void ReportsNotEnoughDataForShortHistory()
    {
        var result = EvaluateMemory(Samples(30, 30));

        Assert.Equal(RecommendationLevel.NotEnoughData, result.Level);
        Assert.Null(result.RecommendedAmount);
    }

    [Fact]
    public void ComponentsWithoutHistoryAreNotEnoughData()
    {
        var results = NextPcAdvisor.CreateDefault().Advise(Inventory, new Dictionary<HardwareComponent, IReadOnlyList<UsageSample>>(), []);

        Assert.All(results, r => Assert.Equal(RecommendationLevel.NotEnoughData, r.Level));
    }

    [Fact]
    public void FormatterExplainsTheFactsAndCauses()
    {
        using var _ = new UiCultureScope("ja-JP");
        var text = new RecommendationTextFormatter().Format(EvaluateMemory(Samples(100, 35)));

        Assert.Equal("メモリ：次の PC は 32GB 以上がおすすめです", text.Title);
        Assert.Contains("80%を超えていた時間", text.Explanation);
        Assert.Contains("35%", text.Explanation);
        Assert.Contains("chromeとTeams", text.Explanation);
    }

    [Theory]
    [InlineData("en-US", "Memory: 32 GB or more is recommended for your next PC", "chrome, Teams")]
    [InlineData("es-ES", "Memoria: se recomienda 32 GB o más para tu próximo PC", "chrome, Teams")]
    [InlineData("fr-FR", "Memory: 32 GB or more is recommended for your next PC", "chrome, Teams")]
    public void FormatterFollowsTheUiLanguage(string culture, string expectedTitle, string expectedCauses)
    {
        using var _ = new UiCultureScope(culture);
        var text = new RecommendationTextFormatter().Format(EvaluateMemory(Samples(100, 35)));

        Assert.Equal(expectedTitle, text.Title);
        Assert.Contains("35%", text.Explanation);
        Assert.Contains(expectedCauses, text.Explanation);
    }

    [Theory]
    [InlineData(4, 4)]
    [InlineData(5, 8)]
    [InlineData(200, 128)]
    public void CapacityTiersRoundUp(int value, int expected)
    {
        Assert.Equal(expected, CapacityTiers.AtLeast(CapacityTiers.MemoryGigabytes, value));
    }
}

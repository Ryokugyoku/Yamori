using Yamori.Core.Analysis;
using Yamori.Core.Analysis.History;
using Yamori.Core.AppUsage.Service;
using Yamori.Core.Hardware;
using Yamori.Core.Processes;
using Yamori.Core.Recommendations;

namespace Yamori.Core.Reports;

/// <summary>
/// ハードウェア一覧・使用率の履歴・アプリの使用量を集めて、<see cref="NextPcReport"/> を作る。
/// </summary>
public sealed class NextPcReportService
{
    /// <summary>
    /// 既定の集計期間（Helper の 1 分集計の保持期間と同じ）。
    /// </summary>
    public static readonly TimeSpan DefaultLookback = TimeSpan.FromDays(90);

    /// <summary>
    /// 一覧に出すアプリの件数。
    /// </summary>
    public const int TopAppCount = 5;

    /// <summary>
    /// 高負荷の時間の CPU の原因として選ぶアプリの、高負荷の時間の平均の使用率の下限（%）。
    /// </summary>
    public const double MinimumCausePercent = 1;

    private readonly IHardwareInventoryProvider _inventoryProvider;
    private readonly IUsageHistoryReader _historyReader;
    private readonly IProcessSnapshotProvider _processProvider;
    private readonly IAppUsageService _appUsage;
    private readonly NextPcAdvisor _advisor;
    private readonly TimeProvider _timeProvider;
    private readonly RecommendationOptions _options;

    /// <summary>
    /// 材料の取得元とおすすめの判定処理を指定して作る。
    /// </summary>
    /// <param name="inventoryProvider">ハードウェア一覧の取得元。</param>
    /// <param name="historyReader">使用率の履歴の読み取り元。</param>
    /// <param name="processProvider">プロセスの使用量の取得元。</param>
    /// <param name="appUsage">アプリごとの使用率の記録の取得元（CPU の原因アプリに使う）。</param>
    /// <param name="advisor">おすすめの判定処理。</param>
    /// <param name="timeProvider">現在時刻の提供元。</param>
    /// <param name="options">判定の基準値（原因アプリを探す高負荷のしきい値に使う）。<paramref name="advisor"/> と同じものを渡すこと。省略時は <see cref="RecommendationOptions.Default"/>。</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> 以外の引数が null。</exception>
    public NextPcReportService(
        IHardwareInventoryProvider inventoryProvider,
        IUsageHistoryReader historyReader,
        IProcessSnapshotProvider processProvider,
        IAppUsageService appUsage,
        NextPcAdvisor advisor,
        TimeProvider timeProvider,
        RecommendationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(inventoryProvider);
        ArgumentNullException.ThrowIfNull(historyReader);
        ArgumentNullException.ThrowIfNull(processProvider);
        ArgumentNullException.ThrowIfNull(appUsage);
        ArgumentNullException.ThrowIfNull(advisor);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _inventoryProvider = inventoryProvider;
        _historyReader = historyReader;
        _processProvider = processProvider;
        _appUsage = appUsage;
        _advisor = advisor;
        _timeProvider = timeProvider;
        _options = options ?? RecommendationOptions.Default;
    }

    /// <summary>
    /// レポートを作る。WMI やファイル、Helper の時系列を読むため時間がかかることがある。UI スレッドからは別スレッドで呼ぶ。
    /// </summary>
    /// <param name="lookback">さかのぼる期間。省略時は <see cref="DefaultLookback"/>。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>作成したレポート。</returns>
    /// <exception cref="IOException">履歴の読み込みに失敗した。</exception>
    public async Task<NextPcReport> CreateAsync(TimeSpan? lookback = null, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var from = now - (lookback ?? DefaultLookback);

        var inventory = _inventoryProvider.GetInventory();
        var topApps = ProcessUsageAggregator.TopByMemory(_processProvider.Capture(), TopAppCount);

        var histories = new Dictionary<HardwareComponent, UsageHistory>();
        foreach (var component in _advisor.Components.Distinct())
        {
            histories[component] = await _historyReader.ReadAsync(component, from, now, cancellationToken).ConfigureAwait(false);
        }

        var cpuApps = await _appUsage
            .GetCpuUsageAsync(from, now, _options.HighLoadThresholdPercent, TopAppCount, cancellationToken)
            .ConfigureAwait(false);
        var causes = new Dictionary<HardwareComponent, IReadOnlyList<string>>
        {
            [HardwareComponent.Cpu] = cpuApps.TopHighLoadApps(TopAppCount, MinimumCausePercent),
        };

        var recommendations = _advisor.Advise(
            inventory,
            histories.ToDictionary(h => h.Key, h => h.Value.Samples),
            topApps,
            causes);
        return new NextPcReport(
            inventory,
            recommendations,
            topApps,
            now,
            histories.ToDictionary(h => h.Key, h => h.Value.Origin));
    }
}

using Yamori.Core.Analysis;
using Yamori.Core.Hardware;
using Yamori.Core.Processes;
using Yamori.Core.Recommendations;
using Yamori.Core.Storage;

namespace Yamori.Core.Reports;

/// <summary>
/// ハードウェア一覧・使用率の履歴・アプリの使用量を集めて、<see cref="NextPcReport"/> を作る。
/// </summary>
public sealed class NextPcReportService
{
    /// <summary>
    /// 既定の集計期間。
    /// </summary>
    public static readonly TimeSpan DefaultLookback = TimeSpan.FromDays(90);

    /// <summary>
    /// 一覧に出すアプリの件数。
    /// </summary>
    public const int TopAppCount = 5;

    private readonly IHardwareInventoryProvider _inventoryProvider;
    private readonly IUsageHistoryStore _historyStore;
    private readonly IProcessSnapshotProvider _processProvider;
    private readonly NextPcAdvisor _advisor;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// 材料の取得元とおすすめの判定処理を指定して作る。
    /// </summary>
    /// <param name="inventoryProvider">ハードウェア一覧の取得元。</param>
    /// <param name="historyStore">使用率の履歴の保存先。</param>
    /// <param name="processProvider">プロセスの使用量の取得元。</param>
    /// <param name="advisor">おすすめの判定処理。</param>
    /// <param name="timeProvider">現在時刻の提供元。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public NextPcReportService(
        IHardwareInventoryProvider inventoryProvider,
        IUsageHistoryStore historyStore,
        IProcessSnapshotProvider processProvider,
        NextPcAdvisor advisor,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(inventoryProvider);
        ArgumentNullException.ThrowIfNull(historyStore);
        ArgumentNullException.ThrowIfNull(processProvider);
        ArgumentNullException.ThrowIfNull(advisor);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _inventoryProvider = inventoryProvider;
        _historyStore = historyStore;
        _processProvider = processProvider;
        _advisor = advisor;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// レポートを作る。WMI やファイルを読むため時間がかかることがある。UI スレッドからは別スレッドで呼ぶ。
    /// </summary>
    /// <param name="lookback">さかのぼる期間。省略時は <see cref="DefaultLookback"/>。</param>
    /// <returns>作成したレポート。</returns>
    /// <exception cref="IOException">履歴の読み込みに失敗した。</exception>
    public NextPcReport Create(TimeSpan? lookback = null)
    {
        var now = _timeProvider.GetUtcNow();
        var from = now - (lookback ?? DefaultLookback);

        var inventory = _inventoryProvider.GetInventory();
        var topApps = ProcessUsageAggregator.TopByMemory(_processProvider.Capture(), TopAppCount);
        var samples = _advisor.Components
            .Distinct()
            .ToDictionary(
                c => c,
                c => (IReadOnlyList<UsageSample>)_historyStore.Read(c, from, now).Select(s => s.ToUsageSample()).ToArray());

        var recommendations = _advisor.Advise(inventory, samples, topApps);
        return new NextPcReport(inventory, recommendations, topApps, now);
    }
}

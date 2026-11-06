using System;
using Yamori.Core.Formatting;
using Yamori.Core.Hardware;
using Yamori.Core.Hardware.Wmi;
using Yamori.Core.Monitoring;
using Yamori.Core.Processes;
using Yamori.Core.Recommendations;
using Yamori.Core.Reports;
using Yamori.Core.Storage;
using Yamori.Presentation;
using Yamori.ViewModels;

namespace Yamori.Composition;

/// <summary>
/// アプリ全体で使う部品を組み立てる場所（コンポジションルート）。依存関係の組み立てはここだけで行う。
/// </summary>
public sealed class AppServices
{
    private AppServices(UsageMonitor monitor, NextPcReportService reportService, NextPcReportPresenter presenter, string historyDirectory)
    {
        Monitor = monitor;
        ReportService = reportService;
        Presenter = presenter;
        HistoryDirectory = historyDirectory;
    }

    /// <summary>
    /// 使用率を記録し続ける監視処理を取得する。
    /// </summary>
    public UsageMonitor Monitor { get; }

    /// <summary>
    /// 次の PC のレポートを作る処理を取得する。
    /// </summary>
    public NextPcReportService ReportService { get; }

    /// <summary>
    /// レポートを表示用に変換する処理を取得する。
    /// </summary>
    public NextPcReportPresenter Presenter { get; }

    /// <summary>
    /// 履歴を保存しているフォルダーを取得する。
    /// </summary>
    public string HistoryDirectory { get; }

    /// <summary>
    /// 標準の構成で部品を組み立てる。
    /// </summary>
    /// <returns>組み立てた部品。</returns>
    public static AppServices Create()
    {
        var timeProvider = TimeProvider.System;
        var historyDirectory = YamoriDataPaths.GetHistoryDirectory();
        var historyStore = new CsvUsageHistoryStore(historyDirectory, new UsageSampleCsvSerializer());

        var monitor = new UsageMonitor([new CpuUsageSensor(), new MemoryUsageSensor()], historyStore, timeProvider);
        var reportService = new NextPcReportService(
            HardwareInventoryService.CreateForWmi(new ManagementWmiQueryExecutor()),
            historyStore,
            new SystemProcessSnapshotProvider(),
            NextPcAdvisor.CreateDefault(),
            timeProvider);

        return new AppServices(monitor, reportService, new NextPcReportPresenter(new RecommendationTextFormatter()), historyDirectory);
    }

    /// <summary>
    /// メイン画面のビューモデルを作る。UI スレッドから呼ぶこと。
    /// </summary>
    /// <returns>新しいビューモデル。</returns>
    public MainViewModel CreateMainViewModel() => new(ReportService, Monitor, Presenter, HistoryDirectory);
}

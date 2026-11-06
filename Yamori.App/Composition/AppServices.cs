using System;
using Microsoft.UI;
using Yamori.Accounts;
using Yamori.Core.Formatting;
using Yamori.Core.Hardware;
using Yamori.Core.Hardware.Wmi;
using Yamori.Core.Monitoring;
using Yamori.Core.Processes;
using Yamori.Core.Recommendations;
using Yamori.Core.Reports;
using Yamori.Core.Settings;
using Yamori.Core.Storage;
using Yamori.Localization;
using Yamori.Presentation;
using Yamori.Services;
using Yamori.ViewModels;

namespace Yamori.Composition;

/// <summary>
/// アプリ全体で使う部品を組み立てる場所（コンポジションルート）。依存関係の組み立てはここだけで行う。
/// </summary>
public sealed class AppServices
{
    private AppServices(
        ISettingsStore settingsStore,
        LanguageService languageService,
        HistoryStorageService storage,
        IOneDriveLocator oneDriveLocator,
        IWindowsAccountProvider accountProvider,
        UsageMonitor monitor,
        NextPcReportService reportService,
        NextPcReportPresenter presenter)
    {
        SettingsStore = settingsStore;
        LanguageService = languageService;
        Storage = storage;
        OneDriveLocator = oneDriveLocator;
        AccountProvider = accountProvider;
        Monitor = monitor;
        ReportService = reportService;
        Presenter = presenter;
    }

    /// <summary>設定の保存先を取得する。</summary>
    public ISettingsStore SettingsStore { get; }

    /// <summary>表示言語を切り替える処理を取得する。</summary>
    public LanguageService LanguageService { get; }

    /// <summary>記録の保存先を管理する処理を取得する。</summary>
    public HistoryStorageService Storage { get; }

    /// <summary>OneDrive フォルダーを探す処理を取得する。</summary>
    public IOneDriveLocator OneDriveLocator { get; }

    /// <summary>サインイン中の Windows アカウントを取得する処理を取得する。</summary>
    public IWindowsAccountProvider AccountProvider { get; }

    /// <summary>使用率を記録し続ける監視処理を取得する。</summary>
    public UsageMonitor Monitor { get; }

    /// <summary>次の PC のレポートを作る処理を取得する。</summary>
    public NextPcReportService ReportService { get; }

    /// <summary>レポートを表示用に変換する処理を取得する。</summary>
    public NextPcReportPresenter Presenter { get; }

    /// <summary>
    /// 標準の構成で部品を組み立てる。保存されている表示言語もここで反映する。UI を作る前に呼ぶこと。
    /// </summary>
    /// <returns>組み立てた部品。</returns>
    public static AppServices Create()
    {
        var settingsStore = new JsonSettingsStore(YamoriDataPaths.GetSettingsFilePath());
        var settings = settingsStore.Load();

        var languageService = new LanguageService();
        languageService.Apply(settings.Language);

        var oneDriveLocator = new OneDriveLocator();
        var serializer = new UsageSampleCsvSerializer();
        var storage = new HistoryStorageService(
            new HistoryLocationResolver(oneDriveLocator, YamoriDataPaths.GetHistoryDirectory(), Environment.MachineName),
            new HistoryMigrator(),
            directory => new CsvUsageHistoryStore(directory, serializer),
            settings);

        var timeProvider = TimeProvider.System;
        var monitor = new UsageMonitor([new CpuUsageSensor(), new MemoryUsageSensor()], storage.Store, timeProvider);
        var reportService = new NextPcReportService(
            HardwareInventoryService.CreateForWmi(new ManagementWmiQueryExecutor()),
            storage.Store,
            new SystemProcessSnapshotProvider(),
            NextPcAdvisor.CreateDefault(),
            timeProvider);

        return new AppServices(
            settingsStore,
            languageService,
            storage,
            oneDriveLocator,
            new CurrentWindowsAccountProvider(),
            monitor,
            reportService,
            new NextPcReportPresenter(new RecommendationTextFormatter()));
    }

    /// <summary>
    /// サインイン画面のビューモデルを作る。
    /// </summary>
    /// <param name="getWindowHandle">Windows Hello の確認画面の親にするウィンドウのハンドルを返す処理。</param>
    /// <returns>新しいビューモデル。</returns>
    public LoginViewModel CreateLoginViewModel(Func<nint> getWindowHandle) =>
        new(AccountProvider, new WindowsHelloAccountVerifier(getWindowHandle));

    /// <summary>
    /// 概要・ハードウェア・アプリの画面のビューモデルを作る。UI スレッドから呼ぶこと。
    /// </summary>
    /// <returns>新しいビューモデル。</returns>
    public MainViewModel CreateMainViewModel() => new(ReportService, Monitor, Presenter, Storage);

    /// <summary>
    /// 設定画面のビューモデルを、保存されている最新の設定で作る。言語を変えたら作り直す。
    /// </summary>
    /// <param name="getWindowId">フォルダー選択画面の親にするウィンドウの ID を返す処理。</param>
    /// <returns>新しいビューモデル。</returns>
    public SettingsViewModel CreateSettingsViewModel(Func<WindowId> getWindowId) =>
        new(
            SettingsStore,
            SettingsStore.Load(),
            Storage,
            OneDriveLocator,
            new WindowsFolderPicker(getWindowId),
            LanguageService,
            AccountProvider.GetCurrent());
}

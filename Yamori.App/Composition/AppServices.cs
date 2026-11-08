using System;
using Yamori.Accounts;
using Yamori.Core.Analysis.History;
using Yamori.Core.Anomalies.Service;
using Yamori.Core.AppUsage.Service;
using Yamori.Core.Formatting;
using Yamori.Core.Hardware;
using Yamori.Core.Hardware.Wmi;
using Yamori.Core.Processes;
using Yamori.Core.Realtime;
using Yamori.Core.Recommendations;
using Yamori.Core.Reports;
using Yamori.Core.Sensors.Display;
using Yamori.Core.Sensors.Ipc;
using Yamori.Core.Sensors.Ipc.Client;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Settings;
using Yamori.Core.Storage;
using Yamori.Localization;
using Yamori.Presentation;
using Yamori.Presentation.Analysis;
using Yamori.Presentation.Hardware;
using Yamori.Presentation.Realtime;
using Yamori.Services;
using Yamori.ViewModels;
using Yamori.ViewModels.Analysis;
using Yamori.ViewModels.Sensors;

namespace Yamori.Composition;

/// <summary>
/// アプリ全体で使う部品を組み立てる場所（コンポジションルート）。依存関係の組み立てはここだけで行う。
/// </summary>
public sealed class AppServices
{
    /// <summary>Helper のパイプにつながるまで待つ最長の時間。</summary>
    private static readonly TimeSpan HelperConnectTimeout = TimeSpan.FromSeconds(1);

    private SensorBoard? _board;

    private AppServices(
        ISettingsStore settingsStore,
        LanguageService languageService,
        IWindowsAccountProvider accountProvider,
        FailoverSensorService sensors,
        IAppUsageService appUsage,
        IAnomalyService anomalies,
        NextPcReportService reportService,
        NextPcReportPresenter presenter,
        HardwareSectionsPresenter hardwarePresenter)
    {
        SettingsStore = settingsStore;
        LanguageService = languageService;
        AccountProvider = accountProvider;
        Sensors = sensors;
        AppUsage = appUsage;
        Anomalies = anomalies;
        ReportService = reportService;
        Presenter = presenter;
        HardwarePresenter = hardwarePresenter;
    }

    /// <summary>設定の保存先を取得する。</summary>
    public ISettingsStore SettingsStore { get; }

    /// <summary>表示言語を切り替える処理を取得する。</summary>
    public LanguageService LanguageService { get; }

    /// <summary>サインイン中の Windows アカウントを取得する処理を取得する。</summary>
    public IWindowsAccountProvider AccountProvider { get; }

    /// <summary>センサーの窓口を取得する。Helper に IPC でつなぎ、Helper が動いていない間は標準 API だけを読んで表示する（記録はしない）。</summary>
    public FailoverSensorService Sensors { get; }

    /// <summary>アプリごとの使用率の記録の窓口を取得する（Helper へのパイプ。Helper が動いていない間は記録なし）。</summary>
    public IAppUsageService AppUsage { get; }

    /// <summary>異常の一覧と設定の窓口を取得する（Helper へのパイプ。Helper が動いていない間は空で、変更はできない）。</summary>
    public IAnomalyService Anomalies { get; }

    /// <summary>次の PC のレポートを作る処理を取得する。</summary>
    public NextPcReportService ReportService { get; }

    /// <summary>レポートを表示用に変換する処理を取得する。</summary>
    public NextPcReportPresenter Presenter { get; }

    /// <summary>ハードウェア一覧を部品の種類ごとの表示内容にする処理を取得する。</summary>
    public HardwareSectionsPresenter HardwarePresenter { get; }

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

        var timeProvider = TimeProvider.System;
        var remote = new RemoteSensorService(SensorPipeConnector.Create(SensorPipeNames.Sensors, HelperConnectTimeout), timeProvider);
        var sensors = new FailoverSensorService(
            remote,
            () => LocalSensorServiceFactory.CreateDisplayOnly(new ManagementWmiQueryExecutor(), YamoriDataPaths.GetSensorPriorityFilePath(), timeProvider));
        // 記録は Helper だけが行う。負荷時間の分析も Helper の時系列（画面を閉じている間も含む）から作る。
        var reportService = new NextPcReportService(
            HardwareInventoryService.CreateDefault(new ManagementWmiQueryExecutor()),
            new SensorUsageHistoryReader(sensors),
            new SystemProcessSnapshotProvider(),
            remote,
            NextPcAdvisor.CreateDefault(RecommendationOptions.Default),
            timeProvider,
            RecommendationOptions.Default);

        return new AppServices(
            settingsStore,
            languageService,
            new CurrentWindowsAccountProvider(),
            sensors,
            remote,
            remote,
            reportService,
            new NextPcReportPresenter(new RecommendationTextFormatter()),
            HardwareSectionsPresenter.CreateDefault());
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
    public MainViewModel CreateMainViewModel() =>
        new(
            ReportService,
            new SummaryCardsViewModel(GetBoard(), SummaryCardDefinitions.Default),
            new AppCpuHistoryViewModel(AppUsage, new AppCpuHistoryPresenter(), RecommendationOptions.Default, TimeProvider.System),
            Presenter,
            HardwarePresenter,
            new SystemSpecPresenter(),
            MachineDataPaths.GetSensorDataDirectory(),
            Sensors,
            AccountProvider.GetCurrent().UserName,
            Environment.MachineName,
            TimeProvider.System);

    /// <summary>
    /// タイトルバーに出す見守りの状態のビューモデルを作る。UI スレッドから呼ぶこと。
    /// </summary>
    /// <returns>新しいビューモデル。</returns>
    public HelperStatusViewModel CreateHelperStatusViewModel() => new(GetBoard(), new ScHelperServiceStarter());

    /// <summary>
    /// リアルタイム画面のビューモデルを作る。UI スレッドから呼ぶこと。アプリごとの使用量の測定の処理は、測定を始めるときに作る。
    /// </summary>
    /// <returns>新しいビューモデル。</returns>
    public RealtimePageViewModel CreateRealtimeViewModel() =>
        new(
            new SensorTreeViewModel(GetBoard(), new HelperElevatedConsentChanger(), new ScHelperServiceStarter()),
            new RealtimeViewModel(
                () => RealtimeSnapshotService.CreateDefault(new ManagementWmiQueryExecutor(), TimeProvider.System),
                RealtimeSectionsPresenter.CreateDefault(),
                TimeProvider.System));

    /// <summary>
    /// 分析画面のビューモデルを作る。UI スレッドから呼ぶこと。画面を開くたびに作る。
    /// </summary>
    /// <returns>新しいビューモデル。</returns>
    public AnalysisViewModel CreateAnalysisViewModel() =>
        new(
            Sensors,
            Anomalies,
            AppUsage,
            Sensors,
            new AnalysisPresenter(),
            new AppCpuHistoryPresenter(),
            RecommendationOptions.Default,
            TimeProvider.System);

    /// <summary>
    /// 概要の「最近の異常」のカードのビューモデルを作る。UI スレッドから呼ぶこと。
    /// </summary>
    /// <returns>新しいビューモデル。</returns>
    public RecentAnomaliesViewModel CreateRecentAnomaliesViewModel() =>
        new(Anomalies, Sensors, new AnalysisPresenter(), TimeProvider.System);

    /// <summary>
    /// 画面で共有するセンサーの状態を取得する。最初に呼んだときに UI スレッドで作る。
    /// </summary>
    private SensorBoard GetBoard() => _board ??= new SensorBoard(Sensors, Sensors);

    /// <summary>
    /// 設定画面のビューモデルを、保存されている最新の設定で作る。言語を変えたら作り直す。
    /// </summary>
    /// <returns>新しいビューモデル。</returns>
    public SettingsViewModel CreateSettingsViewModel() =>
        new(
            SettingsStore,
            SettingsStore.Load(),
            LanguageService,
            AccountProvider.GetCurrent(),
            MachineDataPaths.GetSensorDataDirectory(),
            new NotificationSettingsViewModel(Anomalies, Sensors));
}

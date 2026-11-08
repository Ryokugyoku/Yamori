using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Yamori.Core.Reports;
using Yamori.Core.Sensors.Ipc.Client;
using Yamori.Localization;
using Yamori.Presentation;
using Yamori.Presentation.Hardware;
using Yamori.ViewModels.Sensors;

namespace Yamori.ViewModels;

/// <summary>
/// 概要・ハードウェア・アプリの各画面で共有する状態。サマリーカード、ハードウェア一覧、次の PC のおすすめ、アプリの使用量を持つ。
/// </summary>
public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly NextPcReportService _reportService;
    private readonly NextPcReportPresenter _presenter;
    private readonly HardwareSectionsPresenter _hardwarePresenter;
    private readonly SystemSpecPresenter _specPresenter;
    private readonly string _sensorDataDirectory;
    private readonly IHelperConnection _helperConnection;
    private readonly string _userName;
    private readonly string _machineName;
    private readonly TimeProvider _timeProvider;
    private readonly SynchronizationContext _uiContext;

    /// <summary>概要画面に出す、メモリを多く使っているアプリの数。</summary>
    private const int TopAppsPreviewCount = 5;

    private string _statusText;
    private string _greetingText = string.Empty;
    private string _watchingText = string.Empty;
    private string _privacyNoteText = string.Empty;
    private string _dataDirectoryText = string.Empty;
    private bool _isBusy;
    private bool _refreshAgain;

    /// <summary>
    /// 必要な処理を指定して作る。UI スレッドから呼ぶこと（Helper の接続の通知を UI スレッドへ戻すため）。
    /// </summary>
    /// <param name="reportService">レポートを作る処理。</param>
    /// <param name="summaryCards">概要画面のサマリーカード。このインスタンスが破棄する。</param>
    /// <param name="presenter">レポートを表示用に変換する処理。</param>
    /// <param name="hardwarePresenter">ハードウェア一覧を部品の種類ごとの表示内容にする処理。</param>
    /// <param name="specPresenter">ハードウェア一覧から主な仕様を作る処理。</param>
    /// <param name="sensorDataDirectory">Helper が記録を保存するフォルダー（画面に表示する）。</param>
    /// <param name="helperConnection">Helper との接続。つながったときに、Helper の記録で判定し直すために使う。</param>
    /// <param name="userName">サインイン中のユーザーの名前（あいさつに使う）。</param>
    /// <param name="machineName">この PC の名前。</param>
    /// <param name="timeProvider">今の時刻の提供元（あいさつの時間帯に使う）。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    /// <exception cref="InvalidOperationException">UI スレッド以外から呼ばれた。</exception>
    public MainViewModel(
        NextPcReportService reportService,
        SummaryCardsViewModel summaryCards,
        NextPcReportPresenter presenter,
        HardwareSectionsPresenter hardwarePresenter,
        SystemSpecPresenter specPresenter,
        string sensorDataDirectory,
        IHelperConnection helperConnection,
        string userName,
        string machineName,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(reportService);
        ArgumentNullException.ThrowIfNull(summaryCards);
        ArgumentNullException.ThrowIfNull(presenter);
        ArgumentNullException.ThrowIfNull(hardwarePresenter);
        ArgumentNullException.ThrowIfNull(specPresenter);
        ArgumentNullException.ThrowIfNull(sensorDataDirectory);
        ArgumentNullException.ThrowIfNull(helperConnection);
        ArgumentNullException.ThrowIfNull(userName);
        ArgumentNullException.ThrowIfNull(machineName);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _reportService = reportService;
        SummaryCards = summaryCards;
        _presenter = presenter;
        _hardwarePresenter = hardwarePresenter;
        _specPresenter = specPresenter;
        _sensorDataDirectory = sensorDataDirectory;
        _helperConnection = helperConnection;
        _userName = userName;
        _machineName = machineName;
        _timeProvider = timeProvider;
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException("MainViewModel は UI スレッドで作成してください。");

        _statusText = UiText.Get("Status_Loading");
        UpdateGreeting();
        UpdateDataTexts();
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        _helperConnection.ConnectionStateChanged += OnHelperConnectionStateChanged;
    }

    /// <summary>概要画面のサマリーカードを取得する。</summary>
    public SummaryCardsViewModel SummaryCards { get; }

    /// <summary>ハードウェア一覧を、部品の種類ごとにまとめた表示内容を取得する。</summary>
    public ObservableCollection<HardwareSection> Hardware { get; } = [];

    /// <summary>次の PC のおすすめを取得する。</summary>
    public ObservableCollection<RecommendationItem> Recommendations { get; } = [];

    /// <summary>メモリを多く使っているアプリを取得する。</summary>
    public ObservableCollection<AppUsageItem> TopApps { get; } = [];

    /// <summary>概要画面に出す、メモリを多く使っているアプリの上位を取得する。</summary>
    public ObservableCollection<AppUsageItem> TopAppsPreview { get; } = [];

    /// <summary>この PC の主な仕様を取得する。</summary>
    public ObservableCollection<SpecItem> Specs { get; } = [];

    /// <summary>時間帯に合わせたあいさつを取得する。</summary>
    public string GreetingText
    {
        get => _greetingText;
        private set => SetProperty(ref _greetingText, value);
    }

    /// <summary>「この PC を見守っています」の文を取得する。</summary>
    public string WatchingText
    {
        get => _watchingText;
        private set => SetProperty(ref _watchingText, value);
    }

    /// <summary>表示を最新にするコマンドを取得する。</summary>
    public IAsyncRelayCommand RefreshCommand { get; }

    /// <summary>Helper が記録を保存するフォルダーを示す文字列を取得する。</summary>
    public string DataDirectoryText
    {
        get => _dataDirectoryText;
        private set => SetProperty(ref _dataDirectoryText, value);
    }

    /// <summary>通信とデータの扱いについての説明を取得する。</summary>
    public string PrivacyNoteText
    {
        get => _privacyNoteText;
        private set => SetProperty(ref _privacyNoteText, value);
    }

    /// <summary>最後に更新した時刻などの状態表示を取得する。</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>読み込み中かどうかを取得する。</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// センサーと Helper との接続のイベント購読を解除する。
    /// </summary>
    public void Dispose()
    {
        SummaryCards.Dispose();
        _helperConnection.ConnectionStateChanged -= OnHelperConnectionStateChanged;
    }

    /// <summary>
    /// 表示言語が変わったときに、文字列をすべて作り直す。
    /// </summary>
    /// <returns>作り直し終わったときに完了するタスク。</returns>
    public Task ReloadForLanguageAsync()
    {
        UpdateGreeting();
        UpdateDataTexts();
        SummaryCards.Refresh();
        return RefreshCommand.ExecuteAsync(null);
    }

    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            // WMI と Helper の記録の読み込みは時間がかかるため、UI スレッドを止めないよう別スレッドで行う。
            var report = await Task.Run(() => _reportService.CreateAsync());

            Replace(Hardware, _hardwarePresenter.Build(report.Inventory));
            Replace(Recommendations, _presenter.BuildRecommendations(report));
            Replace(TopApps, _presenter.BuildTopApps(report.TopApps));
            Replace(TopAppsPreview, TopApps.Take(TopAppsPreviewCount));
            Replace(Specs, _specPresenter.Build(report.Inventory));
            UpdateGreeting();
            StatusText = UiText.Format("Status_LastUpdated", report.GeneratedAt.ToLocalTime().ToString("G"));
        }
        catch (IOException ex)
        {
            StatusText = UiText.Format("Status_LoadFailed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }

        if (_refreshAgain)
        {
            _refreshAgain = false;
            _uiContext.Post(_ => _ = RefreshCommand.ExecuteAsync(null), null);
        }
    }

    private void UpdateGreeting()
    {
        GreetingText = GreetingFormatter.For(_timeProvider.GetLocalNow(), _userName);
        WatchingText = UiText.Format("Overview_Watching", _machineName);
    }

    private void UpdateDataTexts()
    {
        DataDirectoryText = UiText.Format("Storage_PathLabel", _sensorDataDirectory);
        PrivacyNoteText = UiText.Get("Privacy_Local");
    }

    /// <summary>
    /// Helper につながったら、Helper の記録（画面を閉じている間も含む）で次の PC の基準を判定し直す。
    /// 更新の途中なら、終わってからもう一度更新する。
    /// </summary>
    private void OnHelperConnectionStateChanged(object? sender, HelperConnectionState state)
    {
        if (state != HelperConnectionState.Connected)
        {
            return;
        }

        _uiContext.Post(
            _ =>
            {
                if (RefreshCommand.IsRunning)
                {
                    _refreshAgain = true;
                }
                else
                {
                    _ = RefreshCommand.ExecuteAsync(null);
                }
            },
            null);
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}

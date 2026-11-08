using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Yamori.Controls;
using Yamori.Core.Analysis.Parts;
using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Service;
using Yamori.Core.AppUsage.Analysis;
using Yamori.Core.AppUsage.Service;
using Yamori.Core.Recommendations;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Ipc.Client;
using Yamori.Core.Sensors.Service;
using Yamori.Localization;
using Yamori.Presentation;
using Yamori.Presentation.Analysis;
using Yamori.Services;

namespace Yamori.ViewModels.Analysis;

/// <summary>
/// 分析画面の状態。左で「すべての異常」か部品の種類を選び、右に期間の中の異常の一覧、負荷・温度・クロックのグラフ、集計値、
/// その範囲に CPU を多く使ったアプリを出す。異常を選ぶと、その区間を中心に表示し、判断の理由を一文で示す。
/// </summary>
/// <remarks>
/// UI スレッドで作り、UI スレッドから使うこと。画面に出している間だけ <see cref="Activate"/> で異常の知らせと Helper の接続を見張る。
/// 読み込みは順番に番号を振り、読んでいる間に選択が変わったら古い結果は捨てる。
/// </remarks>
public sealed class AnalysisViewModel : ObservableObject
{
    /// <summary>1 回に読む異常の最大の数。</summary>
    public const int AnomalyLimit = 1000;

    /// <summary>出すアプリの数。</summary>
    public const int AppCount = 5;

    private readonly ISensorService _sensors;
    private readonly IAnomalyService _anomalies;
    private readonly IAppUsageService _appUsage;
    private readonly IHelperConnection _connection;
    private readonly AnalysisPresenter _presenter;
    private readonly AppCpuHistoryPresenter _appPresenter;
    private readonly RecommendationOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SynchronizationContext _uiContext;

    private IReadOnlyList<AnomalyRecord> _periodAnomalies = [];
    private IReadOnlyDictionary<AnalysisPart, IReadOnlyList<AnalysisHardware>> _hardware = new Dictionary<AnalysisPart, IReadOnlyList<AnalysisHardware>>();
    private IReadOnlyCollection<SensorKey> _catalogKeys = [];
    private double? _manualThreshold;
    private int _listVersion;
    private int _detailVersion;
    private bool _isApplying;
    private long? _pendingAnomalyId;

    private AnalysisPartItem _selectedPart;
    private int _selectedPeriodIndex;
    private DateTimeOffset? _customFrom;
    private DateTimeOffset? _customTo;
    private bool _includeDismissed;
    private AnalysisHardwareOption? _selectedHardware;
    private AnomalyItem? _selectedAnomaly;
    private ChartData? _chart;
    private string _statusText = string.Empty;
    private string _listEmptyText = string.Empty;
    private string _detailTitle = string.Empty;
    private string _detailRangeText = string.Empty;
    private string _sourceText = string.Empty;
    private string _highTemperatureText = string.Empty;
    private string _explanationText = string.Empty;
    private string _appsHeader = string.Empty;
    private string _appsEmptyText = string.Empty;
    private bool _isBusy;
    private bool _hasDetail;

    /// <summary>
    /// 必要な部品を指定して作る。
    /// </summary>
    /// <param name="sensors">センサーの窓口（カタログと記録）。</param>
    /// <param name="anomalies">異常の一覧の窓口。</param>
    /// <param name="appUsage">アプリごとの CPU の記録の窓口。</param>
    /// <param name="connection">Helper との接続の状態。</param>
    /// <param name="presenter">表示内容を作る処理。</param>
    /// <param name="appPresenter">アプリの一覧の表示内容を作る処理。</param>
    /// <param name="options">判定の基準値（高負荷のしきい値）。</param>
    /// <param name="timeProvider">今の時刻の提供元。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    /// <exception cref="InvalidOperationException">UI スレッド以外から呼ばれた。</exception>
    public AnalysisViewModel(
        ISensorService sensors,
        IAnomalyService anomalies,
        IAppUsageService appUsage,
        IHelperConnection connection,
        AnalysisPresenter presenter,
        AppCpuHistoryPresenter appPresenter,
        RecommendationOptions options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(sensors);
        ArgumentNullException.ThrowIfNull(anomalies);
        ArgumentNullException.ThrowIfNull(appUsage);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(presenter);
        ArgumentNullException.ThrowIfNull(appPresenter);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _sensors = sensors;
        _anomalies = anomalies;
        _appUsage = appUsage;
        _connection = connection;
        _presenter = presenter;
        _appPresenter = appPresenter;
        _options = options;
        _timeProvider = timeProvider;
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException("AnalysisViewModel は UI スレッドで作成してください。");

        foreach (var item in presenter.CreatePartItems())
        {
            AllParts.Add(item);
        }

        _selectedPart = AllParts[0];
        var today = DateTimeOffset.Now.Date;
        _customFrom = new DateTimeOffset(today.AddDays(-6));
        _customTo = new DateTimeOffset(today);
        UpdatePartsVisibility();
    }

    /// <summary>左の一覧に出す行（「すべての異常」と部品の種類。異常があるときだけ出す部品は、数が 0 なら含めない）を取得する。</summary>
    public ObservableCollection<AnalysisPartItem> Parts { get; } = [];

    /// <summary>選んでいる行を取得または設定する。</summary>
    public AnalysisPartItem SelectedPart
    {
        get => _selectedPart;
        set
        {
            if (value is null || !SetProperty(ref _selectedPart, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsAllSelected));
            if (!_isApplying)
            {
                ClearSelectedAnomaly();
                ApplyHardwareOptions();
                ApplyList();
                _ = LoadDetailAsync();
            }
        }
    }

    /// <summary>「すべての異常」を選んでいるかどうかを取得する。</summary>
    public bool IsAllSelected => _selectedPart.IsAll;

    /// <summary>選んでいる期間の番号（0：24 時間、1：7 日、2：90 日、3：範囲を指定）を取得または設定する。変えると読み直す。</summary>
    public int SelectedPeriodIndex
    {
        get => _selectedPeriodIndex;
        set
        {
            if (value < 0 || value > (int)AnalysisPeriod.Custom)
            {
                // 画面を作り直す途中などで選択が外れたときは、前の期間のままにする。
                OnPropertyChanged();
                return;
            }

            if (SetProperty(ref _selectedPeriodIndex, value))
            {
                OnPropertyChanged(nameof(IsCustomPeriod));
                if (!_isApplying)
                {
                    ClearSelectedAnomaly();
                    _ = RefreshAsync();
                }
            }
        }
    }

    /// <summary>範囲を日付で指定しているかどうかを取得する。</summary>
    public bool IsCustomPeriod => (AnalysisPeriod)_selectedPeriodIndex == AnalysisPeriod.Custom;

    /// <summary>範囲を指定するときの始まりの日を取得または設定する。</summary>
    public DateTimeOffset? CustomFrom
    {
        get => _customFrom;
        set
        {
            if (SetProperty(ref _customFrom, value) && IsCustomPeriod && value is not null)
            {
                _ = RefreshAsync();
            }
        }
    }

    /// <summary>範囲を指定するときの終わりの日（その日を含む）を取得または設定する。</summary>
    public DateTimeOffset? CustomTo
    {
        get => _customTo;
        set
        {
            if (SetProperty(ref _customTo, value) && IsCustomPeriod && value is not null)
            {
                _ = RefreshAsync();
            }
        }
    }

    /// <summary>日付を選べる最も前の日（1 分集計の保持期間）を取得する。</summary>
    public DateTimeOffset MinimumDate => DateTimeOffset.Now - AnalysisRanges.MaxLookback;

    /// <summary>日付を選べる最も後の日（今日）を取得する。</summary>
    public DateTimeOffset MaximumDate => DateTimeOffset.Now;

    /// <summary>「これは正常」とした異常も一覧に出すかどうかを取得または設定する。</summary>
    public bool IncludeDismissed
    {
        get => _includeDismissed;
        set
        {
            if (SetProperty(ref _includeDismissed, value))
            {
                _ = RefreshAsync();
            }
        }
    }

    /// <summary>同じ種類の部品が複数あるときの選択肢を取得する。</summary>
    public ObservableCollection<AnalysisHardwareOption> HardwareOptions { get; } = [];

    /// <summary>同じ種類の部品が複数あり、選べるかどうかを取得する。</summary>
    public bool HasHardwareChoice => !IsAllSelected && HardwareOptions.Count > 1;

    /// <summary>分析する部品の選択肢を取得または設定する。</summary>
    public AnalysisHardwareOption? SelectedHardware
    {
        get => _selectedHardware;
        set
        {
            if (value is null || !SetProperty(ref _selectedHardware, value) || _isApplying)
            {
                return;
            }

            ClearSelectedAnomaly();
            ApplyList();
            _ = LoadDetailAsync();
        }
    }

    /// <summary>異常の一覧（新しい順）を取得する。</summary>
    public ObservableCollection<AnomalyItem> Anomalies { get; } = [];

    /// <summary>一覧に異常があるかどうかを取得する。</summary>
    public bool HasAnomalies => Anomalies.Count > 0;

    /// <summary>一覧が空のときの説明を出すかどうかを取得する。</summary>
    public bool ShowsListEmpty => !HasAnomalies;

    /// <summary>一覧が空のときの説明を取得する。</summary>
    public string ListEmptyText
    {
        get => _listEmptyText;
        private set => SetProperty(ref _listEmptyText, value);
    }

    /// <summary>選んでいる異常を取得または設定する。選ぶと、その区間を中心にグラフを出す。</summary>
    public AnomalyItem? SelectedAnomaly
    {
        get => _selectedAnomaly;
        set
        {
            if (!SetProperty(ref _selectedAnomaly, value))
            {
                return;
            }

            OnAnomalySelectionChanged();
            if (!_isApplying)
            {
                _ = LoadDetailAsync();
            }
        }
    }

    /// <summary>異常を選んでいるかどうかを取得する。</summary>
    public bool HasSelectedAnomaly => _selectedAnomaly is not null;

    /// <summary>選んでいる異常の判断の理由（一文）を取得する。</summary>
    public string ExplanationText
    {
        get => _explanationText;
        private set => SetProperty(ref _explanationText, value);
    }

    /// <summary>「これは正常」のボタンの文字（正常とした異常なら、印を外す文字）を取得する。</summary>
    public string DismissButtonText =>
        UiText.Get(_selectedAnomaly?.IsDismissed == true ? "Analysis_Undismiss" : "Analysis_Dismiss");

    /// <summary>詳細（グラフ・集計値・アプリ）を出すかどうかを取得する。「すべての異常」で異常を選んでいないときは出さない。</summary>
    public bool HasDetail
    {
        get => _hasDetail;
        private set
        {
            if (SetProperty(ref _hasDetail, value))
            {
                OnPropertyChanged(nameof(ShowsSelectHint));
            }
        }
    }

    /// <summary>「すべての異常」で、異常を選ぶよう促す説明を出すかどうかを取得する。</summary>
    public bool ShowsSelectHint => !_hasDetail && IsAllSelected && HasAnomalies;

    /// <summary>詳細の見出し（部品の名前）を取得する。</summary>
    public string DetailTitle
    {
        get => _detailTitle;
        private set => SetProperty(ref _detailTitle, value);
    }

    /// <summary>グラフの範囲の説明を取得する。</summary>
    public string DetailRangeText
    {
        get => _detailRangeText;
        private set => SetProperty(ref _detailRangeText, value);
    }

    /// <summary>どの記録を使ったかの説明を取得する。</summary>
    public string SourceText
    {
        get => _sourceText;
        private set => SetProperty(ref _sourceText, value);
    }

    /// <summary>グラフに描く内容を取得する。</summary>
    public ChartData? Chart
    {
        get => _chart;
        private set => SetProperty(ref _chart, value);
    }

    /// <summary>集計値の表の行を取得する。</summary>
    public ObservableCollection<AnalysisStatItem> Statistics { get; } = [];

    /// <summary>集計値があるかどうかを取得する。</summary>
    public bool HasStatistics => Statistics.Count > 0;

    /// <summary>「高温の時間」の説明を取得する。温度の記録がなければ空。</summary>
    public string HighTemperatureText
    {
        get => _highTemperatureText;
        private set
        {
            if (SetProperty(ref _highTemperatureText, value))
            {
                OnPropertyChanged(nameof(HasHighTemperature));
            }
        }
    }

    /// <summary>「高温の時間」の説明があるかどうかを取得する。</summary>
    public bool HasHighTemperature => _highTemperatureText.Length > 0;

    /// <summary>アプリの一覧の見出し（期間か、異常の区間か）を取得する。</summary>
    public string AppsHeader
    {
        get => _appsHeader;
        private set => SetProperty(ref _appsHeader, value);
    }

    /// <summary>その範囲に CPU を多く使ったアプリを取得する。</summary>
    public ObservableCollection<AppCpuHistoryItem> Apps { get; } = [];

    /// <summary>アプリがあるかどうかを取得する。</summary>
    public bool HasApps => Apps.Count > 0;

    /// <summary>アプリがないときの説明を出すかどうかを取得する。</summary>
    public bool ShowsAppsEmpty => !HasApps;

    /// <summary>アプリがないときの説明を取得する。</summary>
    public string AppsEmptyText
    {
        get => _appsEmptyText;
        private set => SetProperty(ref _appsEmptyText, value);
    }

    /// <summary>Helper につながっていないなどの知らせを取得する。なければ空。</summary>
    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (SetProperty(ref _statusText, value))
            {
                OnPropertyChanged(nameof(HasStatus));
            }
        }
    }

    /// <summary>知らせがあるかどうかを取得する。</summary>
    public bool HasStatus => _statusText.Length > 0;

    /// <summary>読み込み中かどうかを取得する。</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    private ObservableCollection<AnalysisPartItem> AllParts { get; } = [];

    /// <summary>
    /// 画面に出たときに呼ぶ。異常の知らせと Helper の接続を見張り、読み込む。
    /// </summary>
    /// <param name="anomalyId">最初に選んでおく異常の番号（通知や概要のカードから開いたとき）。なければ null。</param>
    public void Activate(long? anomalyId)
    {
        _anomalies.AnomalyChanged += OnAnomalyChanged;
        _connection.ConnectionStateChanged += OnConnectionStateChanged;
        if (anomalyId is not null)
        {
            ShowAnomaly(anomalyId.Value);
            return;
        }

        _ = RefreshAsync();
    }

    /// <summary>
    /// 画面から外れたときに呼ぶ。見張りをやめる。
    /// </summary>
    public void Deactivate()
    {
        _anomalies.AnomalyChanged -= OnAnomalyChanged;
        _connection.ConnectionStateChanged -= OnConnectionStateChanged;
        Interlocked.Increment(ref _listVersion);
        Interlocked.Increment(ref _detailVersion);
    }

    /// <summary>
    /// 指定した異常を選んだ状態にする。「すべての異常」に切り替え、今の期間にその異常がなければ 90 日に広げる。
    /// </summary>
    /// <param name="anomalyId">異常の番号。</param>
    public void ShowAnomaly(long anomalyId)
    {
        _pendingAnomalyId = anomalyId;
        _isApplying = true;
        try
        {
            SelectedPart = AllParts[0];
            ClearSelectedAnomaly();
        }
        finally
        {
            _isApplying = false;
        }

        _ = RefreshAsync();
    }

    /// <summary>
    /// 異常の選択を外し、期間全体の詳細に戻す。
    /// </summary>
    public void ClearSelection()
    {
        ClearSelectedAnomaly();
        _ = LoadDetailAsync();
    }

    /// <summary>
    /// 選んでいる異常の「これは正常」の印を付ける（正常とした異常なら外す）。一覧と数を読み直す。
    /// </summary>
    /// <returns>終わったときに完了するタスク。</returns>
    public async Task ToggleDismissedAsync()
    {
        if (_selectedAnomaly is not { } item)
        {
            return;
        }

        SensorSettingResult result;
        try
        {
            result = await _anomalies.SetDismissedAsync(item.Record.AnomalyId, !item.IsDismissed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorLog.Write(nameof(AnalysisViewModel), ex);
            result = SensorSettingResult.Unavailable;
        }

        if (result != SensorSettingResult.Applied)
        {
            StatusText = UiText.Get("Analysis_DismissFailed");
            return;
        }

        // 正常の印を外したときは、同じ異常を選んだままにする。付けたときは、一覧から消えるので選択を外す。
        _pendingAnomalyId = item.IsDismissed || _includeDismissed ? item.Record.AnomalyId : null;
        await RefreshAsync();
    }

    /// <summary>
    /// 期間の異常とカタログを読み直し、一覧と詳細を作り直す。
    /// </summary>
    /// <returns>終わったときに完了するタスク。</returns>
    public async Task RefreshAsync()
    {
        var version = Interlocked.Increment(ref _listVersion);
        IsBusy = true;
        try
        {
            var now = _timeProvider.GetUtcNow();
            var range = CurrentRange(now);
            var pending = _pendingAnomalyId;

            var anomalies = await Task.Run(() => _anomalies.GetAnomaliesAsync(range.FromUtc, range.ToUtc, includeDismissed: true, AnomalyLimit));
            if (pending is { } id && anomalies.All(a => a.AnomalyId != id))
            {
                // 通知から開いた異常が今の期間にないときは、90 日に広げて探す。
                var wide = AnalysisRanges.ForPeriod(AnalysisPeriod.Last90Days, now);
                var wider = await Task.Run(() => _anomalies.GetAnomaliesAsync(wide.FromUtc, wide.ToUtc, includeDismissed: true, AnomalyLimit));
                if (wider.Any(a => a.AnomalyId == id))
                {
                    anomalies = wider;
                    range = wide;
                    SetPeriodWithoutReload(AnalysisPeriod.Last90Days);
                    if (wider.First(a => a.AnomalyId == id).IsDismissed)
                    {
                        SetIncludeDismissedWithoutReload(true);
                    }
                }
            }

            var catalog = await _sensors.GetCatalogAsync();
            var settings = await _anomalies.GetSettingsAsync();
            if (version != Volatile.Read(ref _listVersion))
            {
                return;
            }

            _catalogKeys = catalog.Select(d => d.Key).ToHashSet();
            _hardware = AnalysisHardwareCatalog.Build(catalog, anomalies);
            _manualThreshold = settings.FirstOrDefault(s => s.Kind == AnomalyKind.ThermalThrottling)?.ManualThreshold;
            _periodAnomalies = _includeDismissed ? anomalies : anomalies.Where(a => !a.IsDismissed || a.AnomalyId == pending).ToArray();
            StatusText = _connection.ConnectionState == HelperConnectionState.Connected ? string.Empty : UiText.Get("Analysis_HelperRequired");

            UpdateCounts(anomalies.Where(a => !a.IsDismissed));
            ApplyHardwareOptions();
            ApplyList();

            _pendingAnomalyId = null;
            if (pending is { } target && Anomalies.FirstOrDefault(a => a.Record.AnomalyId == target) is { } found)
            {
                _isApplying = true;
                try
                {
                    SelectedAnomaly = found;
                }
                finally
                {
                    _isApplying = false;
                }
            }

            await LoadDetailAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // パイプの想定外の失敗。画面は落とさず、前の表示のままにする。
            ErrorLog.Write(nameof(AnalysisViewModel), ex);
            StatusText = UiText.Get("Analysis_HelperRequired");
        }
        finally
        {
            if (version == Volatile.Read(ref _listVersion))
            {
                IsBusy = false;
            }
        }
    }

    private AnalysisRange CurrentRange(DateTimeOffset now)
    {
        var period = (AnalysisPeriod)_selectedPeriodIndex;
        if (period != AnalysisPeriod.Custom)
        {
            return AnalysisRanges.ForPeriod(period, now);
        }

        var from = DateOnly.FromDateTime((_customFrom ?? now.ToLocalTime()).Date);
        var to = DateOnly.FromDateTime((_customTo ?? now.ToLocalTime()).Date);
        return AnalysisRanges.ForDates(from, to, TimeZoneInfo.Local, now);
    }

    private void SetPeriodWithoutReload(AnalysisPeriod period)
    {
        _isApplying = true;
        try
        {
            SelectedPeriodIndex = (int)period;
        }
        finally
        {
            _isApplying = false;
        }
    }

    /// <summary>
    /// 「これは正常」とした異常も出すかどうかを、読み直さずに変える（呼び出し元が続けて一覧を作るため）。
    /// </summary>
    private void SetIncludeDismissedWithoutReload(bool value) => SetProperty(ref _includeDismissed, value, nameof(IncludeDismissed));

    private void UpdateCounts(IEnumerable<AnomalyRecord> anomalies)
    {
        var list = anomalies.ToArray();
        var counts = AnomalyListBuilder.CountByPart(list);
        foreach (var item in AllParts)
        {
            var count = item.Part is { } part ? counts.GetValueOrDefault(part) : list.Length;
            item.SetCount(count, _presenter.CountText(count));
        }

        UpdatePartsVisibility();
    }

    /// <summary>
    /// 異常があるときだけ出す部品（温度ゾーン）を、数に合わせて一覧に出し入れする。
    /// </summary>
    private void UpdatePartsVisibility()
    {
        var visible = AllParts
            .Where(p => p.Part is not { } part || !AnalysisParts.Optional.Contains(part) || p.HasCount || ReferenceEquals(p, _selectedPart))
            .ToArray();
        if (visible.SequenceEqual(Parts))
        {
            return;
        }

        _isApplying = true;
        try
        {
            Parts.Clear();
            foreach (var item in visible)
            {
                Parts.Add(item);
            }
        }
        finally
        {
            _isApplying = false;
        }
    }

    private void ApplyHardwareOptions()
    {
        var options = _selectedPart.Part is { } part && _hardware.TryGetValue(part, out var list)
            ? list.Select(h => new AnalysisHardwareOption(h)).ToArray()
            : [];
        var keep = options.FirstOrDefault(o => o.Hardware.HardwareKey == _selectedHardware?.Hardware.HardwareKey) ?? options.FirstOrDefault();

        _isApplying = true;
        try
        {
            HardwareOptions.Clear();
            foreach (var option in options)
            {
                HardwareOptions.Add(option);
            }

            _selectedHardware = null;
            SelectedHardware = keep;
            OnPropertyChanged(nameof(SelectedHardware));
        }
        finally
        {
            _isApplying = false;
        }

        OnPropertyChanged(nameof(HasHardwareChoice));
    }

    private void ApplyList()
    {
        var hardwareKey = HasHardwareChoice ? _selectedHardware?.Hardware.HardwareKey : null;
        var records = AnomalyListBuilder.Select(_periodAnomalies, _selectedPart.Part, hardwareKey);
        var selectedId = _selectedAnomaly?.Record.AnomalyId;

        _isApplying = true;
        try
        {
            Anomalies.Clear();
            foreach (var record in records)
            {
                Anomalies.Add(_presenter.CreateItem(record));
            }

            SelectedAnomaly = Anomalies.FirstOrDefault(a => a.Record.AnomalyId == selectedId);
        }
        finally
        {
            _isApplying = false;
        }

        OnPropertyChanged(nameof(HasAnomalies));
        OnPropertyChanged(nameof(ShowsListEmpty));
        OnPropertyChanged(nameof(ShowsSelectHint));
        ListEmptyText = UiText.Get(_selectedPart.IsAll ? "Analysis_NoAnomaliesAll" : "Analysis_NoAnomaliesPart");
    }

    private void ClearSelectedAnomaly()
    {
        if (_selectedAnomaly is null)
        {
            return;
        }

        _selectedAnomaly = null;
        OnPropertyChanged(nameof(SelectedAnomaly));
        OnAnomalySelectionChanged();
    }

    private void OnAnomalySelectionChanged()
    {
        ExplanationText = _selectedAnomaly is { } item ? _presenter.Explain(item.Record) : string.Empty;
        OnPropertyChanged(nameof(HasSelectedAnomaly));
        OnPropertyChanged(nameof(DismissButtonText));
    }

    /// <summary>
    /// 詳細（グラフ・集計値・アプリ）を読む。異常を選んでいればその区間を中心に、なければ期間全体を出す。
    /// </summary>
    private async Task LoadDetailAsync()
    {
        var version = Interlocked.Increment(ref _detailVersion);
        var now = _timeProvider.GetUtcNow();
        var selected = _selectedAnomaly?.Record;
        var hardware = selected is not null ? HardwareOf(selected) : _selectedPart.IsAll ? null : _selectedHardware?.Hardware;
        if (hardware is null)
        {
            HasDetail = false;
            Chart = null;
            return;
        }

        var range = selected is not null ? AnalysisRanges.AroundAnomaly(selected, now) : CurrentRange(now);
        HasDetail = true;
        DetailTitle = UiText.Format("Analysis_PartAndName", AnalysisPresenter.PartName(hardware.Part), hardware.Name);
        DetailRangeText = _presenter.RangeText(range);
        AppsHeader = UiText.Get(selected is not null ? "Analysis_AppsHeader_Anomaly" : "Analysis_AppsHeader_Period");
        IsBusy = true;
        try
        {
            var loader = new PartAnalysisLoader(_sensors);
            var appRange = AnalysisRanges.ForAppUsage(range);
            var analysisTask = Task.Run(() => loader.LoadAsync(hardware, range, _catalogKeys, _manualThreshold));
            var appsTask = Task.Run(() => _appUsage.GetCpuUsageAsync(appRange.FromUtc, appRange.ToUtc, _options.HighLoadThresholdPercent, AppCount));
            var analysis = await analysisTask;
            var apps = await appsTask;
            if (version != Volatile.Read(ref _detailVersion))
            {
                return;
            }

            ApplyDetail(analysis, apps, selected);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorLog.Write(nameof(AnalysisViewModel), ex);
        }
        finally
        {
            if (version == Volatile.Read(ref _detailVersion))
            {
                IsBusy = false;
            }
        }
    }

    private void ApplyDetail(PartAnalysis analysis, AppCpuUsageSummary apps, AnomalyRecord? selected)
    {
        var bands = AnomalyListBuilder.Bands(_periodAnomalies.Concat(selected is null ? [] : [selected]).DistinctBy(a => a.AnomalyId), analysis.Hardware.HardwareKey, analysis.Range);
        Chart = analysis.HasData ? _presenter.CreateChart(analysis, bands, selected?.AnomalyId) : null;
        SourceText = AnalysisPresenter.SourceText(analysis);

        Statistics.Clear();
        foreach (var item in _presenter.CreateStatItems(analysis))
        {
            Statistics.Add(item);
        }

        OnPropertyChanged(nameof(HasStatistics));
        HighTemperatureText = _presenter.HighTemperatureText(analysis);

        Apps.Clear();
        foreach (var item in _appPresenter.BuildItems(apps, AppCount))
        {
            Apps.Add(item);
        }

        OnPropertyChanged(nameof(HasApps));
        OnPropertyChanged(nameof(ShowsAppsEmpty));
        AppsEmptyText = UiText.Get(apps.HasData ? "Analysis_AppsNone" : "Apps_CpuHistoryNoData");
    }

    private AnalysisHardware? HardwareOf(AnomalyRecord anomaly)
    {
        if (!AnalysisHardware.TryParseKey(anomaly.HardwareKey, out var kind, out var id) || AnalysisParts.From(kind) is not { } part)
        {
            return null;
        }

        return _hardware.TryGetValue(part, out var list) && list.FirstOrDefault(h => h.HardwareId == id) is { } known
            ? known
            : new AnalysisHardware(part, id, anomaly.HardwareName);
    }

    private void OnAnomalyChanged(object? sender, AnomalyNotice notice)
    {
        // 異常が始まった・終わった・正常の印が変わったときは一覧を読み直す（続いている間の更新は 30 秒ごとに来るため、間引かない）。
        if (notice.Change is AnomalyChange.Started or AnomalyChange.Ended or AnomalyChange.DismissalChanged)
        {
            _uiContext.Post(_ => KeepSelectionAndRefresh(), null);
        }
    }

    private void OnConnectionStateChanged(object? sender, HelperConnectionState state) =>
        _uiContext.Post(_ => KeepSelectionAndRefresh(), null);

    private void KeepSelectionAndRefresh()
    {
        _pendingAnomalyId ??= _selectedAnomaly?.Record.AnomalyId;
        _ = RefreshAsync();
    }
}

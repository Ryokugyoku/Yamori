using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Yamori.Core.AppUsage.Analysis;
using Yamori.Core.AppUsage.Service;
using Yamori.Core.Recommendations;
using Yamori.Localization;
using Yamori.Presentation;
using Yamori.Services;

namespace Yamori.ViewModels;

/// <summary>
/// アプリ画面の「CPU を多く使ったアプリ（記録）」の状態。Helper の記録から、選んだ期間の順位を読む。
/// </summary>
public sealed class AppCpuHistoryViewModel : ObservableObject
{
    /// <summary>一覧に出すアプリの数。</summary>
    public const int ItemCount = 10;

    /// <summary>選べる期間（画面の選択肢と同じ順）。24 時間・7 日・90 日。</summary>
    private static readonly TimeSpan[] Periods = [TimeSpan.FromDays(1), TimeSpan.FromDays(7), TimeSpan.FromDays(90)];

    /// <summary>最初に選んでおく期間（7 日）。</summary>
    private const int DefaultPeriodIndex = 1;

    private readonly IAppUsageService _appUsage;
    private readonly AppCpuHistoryPresenter _presenter;
    private readonly RecommendationOptions _options;
    private readonly TimeProvider _timeProvider;
    private int _selectedPeriodIndex = DefaultPeriodIndex;
    private int _requestVersion;
    private string _summaryText = string.Empty;
    private string _descriptionText = string.Empty;
    private bool _hasItems;

    /// <summary>
    /// 必要な処理を指定して作る。
    /// </summary>
    /// <param name="appUsage">アプリごとの使用率の記録の窓口（Helper へのパイプ）。</param>
    /// <param name="presenter">記録を表示内容にする処理。</param>
    /// <param name="options">判定の基準値（高負荷のしきい値。次の PC のおすすめと同じものを使う）。</param>
    /// <param name="timeProvider">今の時刻の提供元。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public AppCpuHistoryViewModel(
        IAppUsageService appUsage,
        AppCpuHistoryPresenter presenter,
        RecommendationOptions options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(appUsage);
        ArgumentNullException.ThrowIfNull(presenter);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _appUsage = appUsage;
        _presenter = presenter;
        _options = options;
        _timeProvider = timeProvider;
        UpdateTexts();
    }

    /// <summary>CPU を多く使ったアプリ（平均の多い順）を取得する。</summary>
    public ObservableCollection<AppCpuHistoryItem> Items { get; } = [];

    /// <summary>
    /// 選んでいる期間の番号（0：24 時間、1：7 日、2：90 日）を取得または設定する。変えると読み直す。
    /// </summary>
    public int SelectedPeriodIndex
    {
        get => _selectedPeriodIndex;
        set
        {
            if (value < 0 || value >= Periods.Length)
            {
                // 画面を作り直す途中などで選択が外れたときは、前の期間のままにする。
                OnPropertyChanged();
                return;
            }

            if (SetProperty(ref _selectedPeriodIndex, value))
            {
                _ = RefreshAsync();
            }
        }
    }

    /// <summary>記録した時間と、そのうち高負荷だった時間の説明（記録がなければその理由）を取得する。</summary>
    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    /// <summary>この一覧の値の意味の説明を取得する。</summary>
    public string DescriptionText
    {
        get => _descriptionText;
        private set => SetProperty(ref _descriptionText, value);
    }

    /// <summary>一覧に出すアプリがあるかどうかを取得する。</summary>
    public bool HasItems
    {
        get => _hasItems;
        private set => SetProperty(ref _hasItems, value);
    }

    /// <summary>
    /// 選んでいる期間の記録を読み直す。UI スレッドから呼ぶこと。読んでいる間に期間が変わったら、古い結果は捨てる。
    /// </summary>
    /// <returns>読み終わったときに完了するタスク。</returns>
    public async Task RefreshAsync()
    {
        var version = Interlocked.Increment(ref _requestVersion);
        var now = _timeProvider.GetUtcNow();
        var from = now - Periods[_selectedPeriodIndex];

        AppCpuUsageSummary summary;
        try
        {
            summary = await Task.Run(() => _appUsage.GetCpuUsageAsync(from, now, _options.HighLoadThresholdPercent, ItemCount));
        }
        catch (Exception ex)
        {
            // 期間を変えたときは待たずに呼ばれるため、ここで止める。表示は前の内容のままにする。
            ErrorLog.Write(nameof(AppCpuHistoryViewModel), ex);
            return;
        }

        if (version != Volatile.Read(ref _requestVersion))
        {
            return;
        }

        UpdateTexts();
        SummaryText = _presenter.BuildSummaryText(summary);
        Items.Clear();
        foreach (var item in _presenter.BuildItems(summary, ItemCount))
        {
            Items.Add(item);
        }

        HasItems = Items.Count > 0;
    }

    private void UpdateTexts() =>
        DescriptionText = UiText.Format("Apps_CpuHistoryDescription", _options.HighLoadThresholdPercent);
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Yamori.Core.Analysis.Parts;
using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Service;
using Yamori.Core.Sensors.Ipc.Client;
using Yamori.Localization;
using Yamori.Presentation.Analysis;
using Yamori.Services;

namespace Yamori.ViewModels.Analysis;

/// <summary>
/// 概要の画面の「最近の異常」のカード。直近 7 日の異常（「これは正常」としたものを除く）のうち新しいものを数件出し、分析画面への入口にする。
/// </summary>
/// <remarks>UI スレッドで作り、UI スレッドから使うこと。画面に出している間だけ <see cref="Activate"/> で異常の知らせと Helper の接続を見張る。</remarks>
public sealed class RecentAnomaliesViewModel : ObservableObject
{
    /// <summary>カードに出す異常の数。</summary>
    public const int ItemCount = 3;

    private readonly IAnomalyService _anomalies;
    private readonly IHelperConnection _connection;
    private readonly AnalysisPresenter _presenter;
    private readonly TimeProvider _timeProvider;
    private readonly SynchronizationContext _uiContext;
    private int _version;
    private string _summaryText = string.Empty;

    /// <summary>
    /// 必要な部品を指定して作る。
    /// </summary>
    /// <param name="anomalies">異常の一覧の窓口。</param>
    /// <param name="connection">Helper との接続の状態。</param>
    /// <param name="presenter">表示内容を作る処理。</param>
    /// <param name="timeProvider">今の時刻の提供元。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    /// <exception cref="InvalidOperationException">UI スレッド以外から呼ばれた。</exception>
    public RecentAnomaliesViewModel(IAnomalyService anomalies, IHelperConnection connection, AnalysisPresenter presenter, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(anomalies);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(presenter);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _anomalies = anomalies;
        _connection = connection;
        _presenter = presenter;
        _timeProvider = timeProvider;
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException("RecentAnomaliesViewModel は UI スレッドで作成してください。");
        SummaryText = UiText.Get("Overview_AnomaliesLoading");
    }

    /// <summary>
    /// 分析画面を開くよう頼まれたときに発生する。引数は選んでおく異常の番号（カードの見出しから開くときは null）。
    /// </summary>
    public event EventHandler<long?>? OpenRequested;

    /// <summary>新しい順の異常を取得する。</summary>
    public ObservableCollection<AnomalyItem> Items { get; } = [];

    /// <summary>異常があるかどうかを取得する。</summary>
    public bool HasItems => Items.Count > 0;

    /// <summary>直近 7 日の異常の数の説明（記録が読めなければその理由）を取得する。</summary>
    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    /// <summary>
    /// 画面に出たときに呼ぶ。読み込み、異常の知らせと接続の変化で読み直す。
    /// </summary>
    public void Activate()
    {
        _anomalies.AnomalyChanged += OnAnomalyChanged;
        _connection.ConnectionStateChanged += OnConnectionStateChanged;
        _ = RefreshAsync();
    }

    /// <summary>
    /// 画面から外れたときに呼ぶ。見張りをやめる。
    /// </summary>
    public void Deactivate()
    {
        _anomalies.AnomalyChanged -= OnAnomalyChanged;
        _connection.ConnectionStateChanged -= OnConnectionStateChanged;
        Interlocked.Increment(ref _version);
    }

    /// <summary>
    /// 分析画面を開くよう頼む。
    /// </summary>
    /// <param name="anomalyId">選んでおく異常の番号。なければ null。</param>
    public void Open(long? anomalyId) => OpenRequested?.Invoke(this, anomalyId);

    /// <summary>
    /// 直近 7 日の異常を読み直す。
    /// </summary>
    /// <returns>終わったときに完了するタスク。</returns>
    public async Task RefreshAsync()
    {
        var version = Interlocked.Increment(ref _version);
        var range = AnalysisRanges.ForPeriod(AnalysisPeriod.Last7Days, _timeProvider.GetUtcNow());
        try
        {
            var anomalies = await Task.Run(() => _anomalies.GetAnomaliesAsync(range.FromUtc, range.ToUtc, includeDismissed: false, AnalysisViewModel.AnomalyLimit));
            if (version != Volatile.Read(ref _version))
            {
                return;
            }

            Items.Clear();
            foreach (var anomaly in AnomalyListBuilder.Select(anomalies, null).Take(ItemCount))
            {
                Items.Add(_presenter.CreateItem(anomaly));
            }

            OnPropertyChanged(nameof(HasItems));
            SummaryText = _connection.ConnectionState != HelperConnectionState.Connected
                ? UiText.Get("Analysis_HelperRequired")
                : anomalies.Count == 0
                    ? UiText.Get("Overview_AnomaliesNone")
                    : UiText.Format("Overview_AnomaliesCount", anomalies.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorLog.Write(nameof(RecentAnomaliesViewModel), ex);
        }
    }

    private void OnAnomalyChanged(object? sender, AnomalyNotice notice)
    {
        if (notice.Change is AnomalyChange.Started or AnomalyChange.Ended or AnomalyChange.DismissalChanged)
        {
            _uiContext.Post(_ => _ = RefreshAsync(), null);
        }
    }

    private void OnConnectionStateChanged(object? sender, HelperConnectionState state) =>
        _uiContext.Post(_ => _ = RefreshAsync(), null);
}

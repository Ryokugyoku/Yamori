using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Yamori.Core.Hardware;
using Yamori.Core.Monitoring;
using Yamori.Core.Reports;
using Yamori.Presentation;

namespace Yamori.ViewModels;

/// <summary>
/// メイン画面の状態。今の使用率、ハードウェア一覧、次の PC のおすすめ、アプリの使用量を持つ。
/// </summary>
public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly NextPcReportService _reportService;
    private readonly UsageMonitor _monitor;
    private readonly NextPcReportPresenter _presenter;
    private readonly SynchronizationContext _uiContext;

    private string _cpuUsageText = "—";
    private string _memoryUsageText = "—";
    private string _statusText = "読み込み中…";
    private bool _isBusy;

    /// <summary>
    /// 必要な処理を指定して作る。UI スレッドから呼ぶこと（使用率の更新を UI スレッドへ戻すため）。
    /// </summary>
    /// <param name="reportService">レポートを作る処理。</param>
    /// <param name="monitor">使用率の監視処理。</param>
    /// <param name="presenter">レポートを表示用に変換する処理。</param>
    /// <param name="dataDirectory">履歴の保存先フォルダー（画面に表示する）。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    /// <exception cref="InvalidOperationException">UI スレッド以外から呼ばれた。</exception>
    public MainViewModel(NextPcReportService reportService, UsageMonitor monitor, NextPcReportPresenter presenter, string dataDirectory)
    {
        ArgumentNullException.ThrowIfNull(reportService);
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(presenter);
        ArgumentNullException.ThrowIfNull(dataDirectory);

        _reportService = reportService;
        _monitor = monitor;
        _presenter = presenter;
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException("MainViewModel は UI スレッドで作成してください。");

        DataDirectoryText = $"記録はこの PC の中だけに保存されます：{dataDirectory}";
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        _monitor.SamplesRecorded += OnSamplesRecorded;
    }

    /// <summary>ハードウェア一覧の行を取得する。</summary>
    public ObservableCollection<HardwareRow> Hardware { get; } = [];

    /// <summary>次の PC のおすすめを取得する。</summary>
    public ObservableCollection<RecommendationItem> Recommendations { get; } = [];

    /// <summary>メモリを多く使っているアプリを取得する。</summary>
    public ObservableCollection<AppUsageItem> TopApps { get; } = [];

    /// <summary>表示を最新にするコマンドを取得する。</summary>
    public IAsyncRelayCommand RefreshCommand { get; }

    /// <summary>データの保存先の説明を取得する。</summary>
    public string DataDirectoryText { get; }

    /// <summary>今の CPU 使用率の表示を取得する。</summary>
    public string CpuUsageText
    {
        get => _cpuUsageText;
        private set => SetProperty(ref _cpuUsageText, value);
    }

    /// <summary>今のメモリ使用率の表示を取得する。</summary>
    public string MemoryUsageText
    {
        get => _memoryUsageText;
        private set => SetProperty(ref _memoryUsageText, value);
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
    /// 監視処理のイベント購読を解除する。
    /// </summary>
    public void Dispose() => _monitor.SamplesRecorded -= OnSamplesRecorded;

    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            // WMI とファイルの読み込みは時間がかかるため、UI スレッドを止めないよう別スレッドで行う。
            var report = await Task.Run(() => _reportService.Create());

            Replace(Hardware, _presenter.BuildHardwareRows(report.Inventory));
            Replace(Recommendations, _presenter.BuildRecommendations(report.Recommendations));
            Replace(TopApps, _presenter.BuildTopApps(report.TopApps));
            StatusText = $"最終更新：{report.GeneratedAt.ToLocalTime():yyyy/MM/dd HH:mm:ss}";
        }
        catch (IOException ex)
        {
            StatusText = $"記録の読み込みに失敗しました：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnSamplesRecorded(object? sender, IReadOnlyList<ComponentUsageSample> samples)
    {
        _uiContext.Post(
            _ =>
            {
                foreach (var sample in samples)
                {
                    var text = sample.UsagePercent.ToString("0", CultureInfo.InvariantCulture) + "%";
                    switch (sample.Component)
                    {
                        case HardwareComponent.Cpu:
                            CpuUsageText = text;
                            break;
                        case HardwareComponent.Memory:
                            MemoryUsageText = text;
                            break;
                    }
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

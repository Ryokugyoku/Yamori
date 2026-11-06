using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Yamori.Core.Hardware;
using Yamori.Core.Monitoring;
using Yamori.Core.Reports;
using Yamori.Core.Storage;
using Yamori.Localization;
using Yamori.Presentation;

namespace Yamori.ViewModels;

/// <summary>
/// 概要・ハードウェア・アプリの各画面で共有する状態。今の使用率、ハードウェア一覧、次の PC のおすすめ、アプリの使用量を持つ。
/// </summary>
public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly NextPcReportService _reportService;
    private readonly UsageMonitor _monitor;
    private readonly NextPcReportPresenter _presenter;
    private readonly HistoryStorageService _storage;
    private readonly SynchronizationContext _uiContext;

    private double _cpuUsage;
    private double _memoryUsage;
    private bool _hasUsage;
    private string _statusText;
    private string _privacyNoteText = string.Empty;
    private string _dataDirectoryText = string.Empty;
    private bool _isBusy;

    /// <summary>
    /// 必要な処理を指定して作る。UI スレッドから呼ぶこと（使用率の更新を UI スレッドへ戻すため）。
    /// </summary>
    /// <param name="reportService">レポートを作る処理。</param>
    /// <param name="monitor">使用率の監視処理。</param>
    /// <param name="presenter">レポートを表示用に変換する処理。</param>
    /// <param name="storage">記録の保存先（画面に表示する）。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    /// <exception cref="InvalidOperationException">UI スレッド以外から呼ばれた。</exception>
    public MainViewModel(NextPcReportService reportService, UsageMonitor monitor, NextPcReportPresenter presenter, HistoryStorageService storage)
    {
        ArgumentNullException.ThrowIfNull(reportService);
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(presenter);
        ArgumentNullException.ThrowIfNull(storage);

        _reportService = reportService;
        _monitor = monitor;
        _presenter = presenter;
        _storage = storage;
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException("MainViewModel は UI スレッドで作成してください。");

        _statusText = UiText.Get("Status_Loading");
        UpdateLocationTexts();
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        _monitor.SamplesRecorded += OnSamplesRecorded;
        _storage.LocationChanged += OnLocationChanged;
    }

    /// <summary>ハードウェア一覧の項目を取得する。</summary>
    public ObservableCollection<HardwareRow> Hardware { get; } = [];

    /// <summary>次の PC のおすすめを取得する。</summary>
    public ObservableCollection<RecommendationItem> Recommendations { get; } = [];

    /// <summary>メモリを多く使っているアプリを取得する。</summary>
    public ObservableCollection<AppUsageItem> TopApps { get; } = [];

    /// <summary>表示を最新にするコマンドを取得する。</summary>
    public IAsyncRelayCommand RefreshCommand { get; }

    /// <summary>記録の保存先のフォルダーを示す文字列を取得する。</summary>
    public string DataDirectoryText
    {
        get => _dataDirectoryText;
        private set => SetProperty(ref _dataDirectoryText, value);
    }

    /// <summary>通信とデータの扱いについての説明を取得する。保存先によって変わる。</summary>
    public string PrivacyNoteText
    {
        get => _privacyNoteText;
        private set => SetProperty(ref _privacyNoteText, value);
    }

    /// <summary>今の CPU 使用率（%）を取得する。</summary>
    public double CpuUsage
    {
        get => _cpuUsage;
        private set
        {
            if (SetProperty(ref _cpuUsage, value))
            {
                OnPropertyChanged(nameof(CpuUsageText));
            }
        }
    }

    /// <summary>今のメモリ使用率（%）を取得する。</summary>
    public double MemoryUsage
    {
        get => _memoryUsage;
        private set
        {
            if (SetProperty(ref _memoryUsage, value))
            {
                OnPropertyChanged(nameof(MemoryUsageText));
            }
        }
    }

    /// <summary>今の CPU 使用率の表示を取得する。まだ測れていなければ「—」。</summary>
    public string CpuUsageText => FormatPercent(CpuUsage);

    /// <summary>今のメモリ使用率の表示を取得する。まだ測れていなければ「—」。</summary>
    public string MemoryUsageText => FormatPercent(MemoryUsage);

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
    /// 監視処理と保存先のイベント購読を解除する。
    /// </summary>
    public void Dispose()
    {
        _monitor.SamplesRecorded -= OnSamplesRecorded;
        _storage.LocationChanged -= OnLocationChanged;
    }

    /// <summary>
    /// 表示言語が変わったときに、文字列をすべて作り直す。
    /// </summary>
    /// <returns>作り直し終わったときに完了するタスク。</returns>
    public Task ReloadForLanguageAsync()
    {
        UpdateLocationTexts();
        OnPropertyChanged(nameof(CpuUsageText));
        OnPropertyChanged(nameof(MemoryUsageText));
        return RefreshCommand.ExecuteAsync(null);
    }

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
    }

    private string FormatPercent(double value) => _hasUsage ? $"{value:0}%" : "—";

    private void UpdateLocationTexts()
    {
        var location = _storage.CurrentLocation;
        DataDirectoryText = StorageLocationTexts.PathLabel(location);
        PrivacyNoteText = StorageLocationTexts.PrivacyNote(location);
    }

    private void OnLocationChanged(object? sender, HistoryLocation location) =>
        _uiContext.Post(_ => UpdateLocationTexts(), null);

    private void OnSamplesRecorded(object? sender, IReadOnlyList<ComponentUsageSample> samples)
    {
        _uiContext.Post(
            _ =>
            {
                _hasUsage = true;
                foreach (var sample in samples)
                {
                    switch (sample.Component)
                    {
                        case HardwareComponent.Cpu:
                            CpuUsage = sample.UsagePercent;
                            OnPropertyChanged(nameof(CpuUsageText));
                            break;
                        case HardwareComponent.Memory:
                            MemoryUsage = sample.UsagePercent;
                            OnPropertyChanged(nameof(MemoryUsageText));
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

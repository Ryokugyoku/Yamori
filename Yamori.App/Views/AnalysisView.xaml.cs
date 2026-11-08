using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Yamori.Presentation.Analysis;
using Yamori.Services;
using Yamori.ViewModels.Analysis;

namespace Yamori.Views;

/// <summary>
/// 分析画面。左で「すべての異常」か部品の種類を選び、右に異常の一覧・グラフ・集計値・CPU を多く使ったアプリを出す。
/// 一覧の選択はここでビューモデルとつなぐ（ビューモデルが選び直したときも、一覧の選択に映す）。
/// </summary>
public sealed partial class AnalysisView : UserControl, ISelfScrollingPage
{
    private long? _initialAnomalyId;

    /// <summary>
    /// 表示するビューモデルを指定して作る。
    /// </summary>
    /// <param name="viewModel">画面の状態。</param>
    /// <param name="initialAnomalyId">最初に選んでおく異常の番号（通知や概要のカードから開いたとき）。なければ null。</param>
    /// <exception cref="ArgumentNullException"><paramref name="viewModel"/> が null。</exception>
    public AnalysisView(AnalysisViewModel viewModel, long? initialAnomalyId = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        _initialAnomalyId = initialAnomalyId;
        InitializeComponent();
    }

    /// <summary>画面の状態を取得する。x:Bind の参照先。</summary>
    public AnalysisViewModel ViewModel { get; }

    /// <summary>
    /// 開いている画面で、指定した異常を選ぶ（分析画面を開いたまま通知を押したときなど）。
    /// </summary>
    /// <param name="anomalyId">異常の番号。</param>
    public void ShowAnomaly(long anomalyId) => ViewModel.ShowAnomaly(anomalyId);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.Parts.CollectionChanged += OnPartsChanged;
        ViewModel.Activate(_initialAnomalyId);
        _initialAnomalyId = null;
        ShowPartSelection();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Parts.CollectionChanged -= OnPartsChanged;
        ViewModel.Deactivate();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AnalysisViewModel.SelectedPart):
                ShowPartSelection();
                break;
            case nameof(AnalysisViewModel.SelectedAnomaly):
                if (!ReferenceEquals(AnomalyList.SelectedItem, ViewModel.SelectedAnomaly))
                {
                    AnomalyList.SelectedItem = ViewModel.SelectedAnomaly;
                }

                if (ViewModel.SelectedAnomaly is { } selected)
                {
                    AnomalyList.ScrollIntoView(selected);
                }

                break;
            case nameof(AnalysisViewModel.SelectedHardware):
                if (!ReferenceEquals(HardwareBox.SelectedItem, ViewModel.SelectedHardware))
                {
                    HardwareBox.SelectedItem = ViewModel.SelectedHardware;
                }

                break;
        }
    }

    private void OnPartsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        DispatcherQueue.TryEnqueue(ShowPartSelection);

    private void ShowPartSelection()
    {
        if (!ReferenceEquals(PartList.SelectedItem, ViewModel.SelectedPart))
        {
            PartList.SelectedItem = ViewModel.SelectedPart;
        }
    }

    private void OnPartSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PartList.SelectedItem is AnalysisPartItem part)
        {
            ViewModel.SelectedPart = part;
        }
    }

    private void OnHardwareSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HardwareBox.SelectedItem is AnalysisHardwareOption option)
        {
            ViewModel.SelectedHardware = option;
        }
    }

    private void OnAnomalySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 一覧を作り直すときの、選択が外れるだけの知らせは無視する（選択はビューモデルが決め直す）。
        if (AnomalyList.SelectedItem is AnomalyItem item)
        {
            ViewModel.SelectedAnomaly = item;
        }
    }

    private void OnClearSelectionClick(object sender, RoutedEventArgs e)
    {
        AnomalyList.SelectedItem = null;
        ViewModel.ClearSelection();
    }

    /// <summary>
    /// 「これは正常」が押されたら、印を付ける（外す）。
    /// </summary>
    /// <remarks>ボタンの処理（async void）で例外が出るとアプリが落ちるため、ここで止めて書き残す。</remarks>
    private async void OnDismissClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.ToggleDismissedAsync();
        }
        catch (Exception ex)
        {
            ErrorLog.Write(nameof(AnalysisView), ex);
        }
    }
}

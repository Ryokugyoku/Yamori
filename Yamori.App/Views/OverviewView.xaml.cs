using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Yamori.Presentation.Analysis;
using Yamori.ViewModels;
using Yamori.ViewModels.Analysis;

namespace Yamori.Views;

/// <summary>
/// 概要画面。今の使用率、最近の異常（分析画面への入口）、次の PC の基準を表示する。
/// </summary>
public sealed partial class OverviewView : UserControl
{
    /// <summary>
    /// 表示するビューモデルを指定して作る。
    /// </summary>
    /// <param name="viewModel">画面の状態。</param>
    /// <param name="recentAnomalies">「最近の異常」のカードの状態。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public OverviewView(MainViewModel viewModel, RecentAnomaliesViewModel recentAnomalies)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(recentAnomalies);
        ViewModel = viewModel;
        RecentAnomalies = recentAnomalies;
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>画面の状態を取得する。x:Bind の参照先。</summary>
    public MainViewModel ViewModel { get; }

    /// <summary>「最近の異常」のカードの状態を取得する。x:Bind の参照先。</summary>
    public RecentAnomaliesViewModel RecentAnomalies { get; }

    private void OnLoaded(object sender, RoutedEventArgs e) => RecentAnomalies.Activate();

    /// <summary>
    /// 閉じたら、共有のビューモデルの変更を受け取らないようにする（画面は開くたびに作り直す）。
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        RecentAnomalies.Deactivate();
        Bindings.StopTracking();
        DetachedViewCleaner.Release(this);
    }

    private void OnOpenAnalysisClick(object sender, RoutedEventArgs e) => RecentAnomalies.Open(null);

    private void OnRecentAnomalyClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is AnomalyItem item)
        {
            RecentAnomalies.Open(item.Record.AnomalyId);
        }
    }
}

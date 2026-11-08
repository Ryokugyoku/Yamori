using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Yamori.ViewModels;

namespace Yamori.Views;

/// <summary>
/// 概要画面。今の使用率と、次の PC の基準を表示する。
/// </summary>
public sealed partial class OverviewView : UserControl
{
    /// <summary>
    /// 表示するビューモデルを指定して作る。
    /// </summary>
    /// <param name="viewModel">画面の状態。</param>
    /// <exception cref="ArgumentNullException"><paramref name="viewModel"/> が null。</exception>
    public OverviewView(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
        Unloaded += OnUnloaded;
    }

    /// <summary>画面の状態を取得する。x:Bind の参照先。</summary>
    public MainViewModel ViewModel { get; }

    /// <summary>
    /// 閉じたら、共有のビューモデルの変更を受け取らないようにする（画面は開くたびに作り直す）。
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Bindings.StopTracking();
        DetachedViewCleaner.Release(this);
    }
}

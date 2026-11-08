using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Yamori.ViewModels;

namespace Yamori.Views;

/// <summary>
/// アプリ画面。Helper の記録から求めた CPU を多く使ったアプリ（期間を選べる）と、今メモリを多く使っているアプリを、棒グラフつきで表示する。
/// </summary>
public sealed partial class AppsView : UserControl
{
    /// <summary>
    /// 表示するビューモデルを指定して作る。
    /// </summary>
    /// <param name="viewModel">画面の状態。</param>
    /// <exception cref="ArgumentNullException"><paramref name="viewModel"/> が null。</exception>
    public AppsView(MainViewModel viewModel)
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
    /// 期間の選択（TwoWay）も、閉じた画面から書き戻されないよう止める。
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Bindings.StopTracking();
        DetachedViewCleaner.Release(this);
    }
}

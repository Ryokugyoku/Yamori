using System;
using Microsoft.UI.Xaml.Controls;
using Yamori.ViewModels;

namespace Yamori.Views;

/// <summary>
/// アプリ画面。メモリを多く使っているアプリを、使用量の棒グラフつきで表示する。
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
    }

    /// <summary>画面の状態を取得する。x:Bind の参照先。</summary>
    public MainViewModel ViewModel { get; }
}

using System;
using Microsoft.UI.Xaml.Controls;
using Yamori.ViewModels;

namespace Yamori.Views;

/// <summary>
/// ハードウェア画面。部品ごとにカードで表示する。
/// </summary>
public sealed partial class HardwareView : UserControl
{
    /// <summary>
    /// 表示するビューモデルを指定して作る。
    /// </summary>
    /// <param name="viewModel">画面の状態。</param>
    /// <exception cref="ArgumentNullException"><paramref name="viewModel"/> が null。</exception>
    public HardwareView(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>画面の状態を取得する。x:Bind の参照先。</summary>
    public MainViewModel ViewModel { get; }
}

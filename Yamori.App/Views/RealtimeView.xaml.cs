using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Yamori.ViewModels;

namespace Yamori.Views;

/// <summary>
/// リアルタイム画面。カタログから組み立てたセンサーの詳細のツリーと、アプリごとの今の使用量を表示する。
/// アプリごとの使用量は、画面が表示されている間だけ測定し、閉じると止める。
/// </summary>
public sealed partial class RealtimeView : UserControl
{
    /// <summary>
    /// 表示するビューモデルを指定して作る。ビューモデルは、この画面が閉じられたときに破棄する。
    /// </summary>
    /// <param name="viewModel">画面の状態。</param>
    /// <exception cref="ArgumentNullException"><paramref name="viewModel"/> が null。</exception>
    public RealtimeView(RealtimePageViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>画面の状態を取得する。x:Bind の参照先。</summary>
    public RealtimePageViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e) => ViewModel.Start();

    private void OnUnloaded(object sender, RoutedEventArgs e) => ViewModel.Dispose();
}

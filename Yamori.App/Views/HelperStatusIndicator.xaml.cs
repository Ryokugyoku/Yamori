using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Yamori.ViewModels;

namespace Yamori.Views;

/// <summary>
/// タイトルバーに置く「見守りの状態」の札。押すと詳しい状態を出し、Helper が止まっていればサービスを起動できる。
/// </summary>
public sealed partial class HelperStatusIndicator : UserControl
{
    /// <summary>
    /// 表示するビューモデルを指定して作る。
    /// </summary>
    /// <param name="viewModel">見守りの状態。</param>
    /// <exception cref="ArgumentNullException"><paramref name="viewModel"/> が null。</exception>
    public HelperStatusIndicator(HelperStatusViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>見守りの状態を取得する。x:Bind の参照先。</summary>
    public HelperStatusViewModel ViewModel { get; }

    /// <summary>
    /// サービスを起動する。起動できなければ、理由を札の下に出す。
    /// </summary>
    private async void OnStartClick(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        var error = await ViewModel.StartAsync();
        if (error is not null)
        {
            ErrorText.Text = error;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}

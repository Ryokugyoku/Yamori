using System;
using Microsoft.UI.Xaml.Controls;
using Yamori.ViewModels;

namespace Yamori.Views;

/// <summary>
/// 設定画面。アカウント、記録の保存先、表示言語を表示・変更する。
/// </summary>
public sealed partial class SettingsView : UserControl
{
    /// <summary>
    /// 表示するビューモデルを指定して作る。
    /// </summary>
    /// <param name="viewModel">画面の状態。</param>
    /// <exception cref="ArgumentNullException"><paramref name="viewModel"/> が null。</exception>
    public SettingsView(SettingsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>画面の状態を取得する。x:Bind の参照先。</summary>
    public SettingsViewModel ViewModel { get; }
}

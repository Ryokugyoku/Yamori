using System;
using Microsoft.UI.Xaml.Controls;
using Yamori.ViewModels;

namespace Yamori.Views;

/// <summary>
/// サインイン画面。Windows のライト／ダーク モードに合わせて表示が変わる。表示内容は <see cref="LoginViewModel"/> から取得する。
/// </summary>
public sealed partial class LoginView : UserControl
{
    /// <summary>
    /// 表示するビューモデルを指定して作る。
    /// </summary>
    /// <param name="viewModel">画面の状態。</param>
    /// <exception cref="ArgumentNullException"><paramref name="viewModel"/> が null。</exception>
    public LoginView(LoginViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += (_, _) => SignInButton.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
    }

    /// <summary>
    /// 画面の状態を取得する。x:Bind の参照先。
    /// </summary>
    public LoginViewModel ViewModel { get; }
}

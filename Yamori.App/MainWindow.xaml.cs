using System;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using Yamori.ViewModels;

namespace Yamori;

/// <summary>
/// Yamori のメイン画面。表示内容はすべて <see cref="MainViewModel"/> から取得する。
/// </summary>
public sealed partial class MainWindow : Window
{
    private static readonly SizeInt32 InitialSize = new(1040, 820);

    /// <summary>
    /// 表示するビューモデルを指定して作る。
    /// </summary>
    /// <param name="viewModel">画面の状態。</param>
    /// <exception cref="ArgumentNullException"><paramref name="viewModel"/> が null。</exception>
    public MainWindow(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
        AppWindow.Resize(InitialSize);
    }

    /// <summary>
    /// 画面の状態を取得する。x:Bind の参照先。
    /// </summary>
    public MainViewModel ViewModel { get; }
}

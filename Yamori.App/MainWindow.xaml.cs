using System;
using System.IO;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using Yamori.Services;

namespace Yamori;

/// <summary>
/// Yamori のウィンドウ。タイトルバーと、Windows のライト／ダーク モードへの追従を受け持ち、中身（サインイン画面やメイン画面）を差し替えて表示する。
/// </summary>
public sealed partial class MainWindow : Window
{
    private static readonly SizeInt32 InitialSize = new(1180, 820);

    /// <summary>タスクバーや Alt+Tab に表示するウィンドウのアイコン（アプリのフォルダーからの相対パス）。</summary>
    private const string WindowIconPath = @"Assets\Logo\Yamori.ico";

    private readonly SystemThemeWatcher _themeWatcher;

    /// <summary>
    /// ウィンドウを作る。UI スレッドから呼ぶこと。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        // TitleBar は戻るボタンなどを持たないため高さ 32px のコンパクト表示になる。右上のボタンも同じ高さにそろえる。
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
        AppWindow.Resize(InitialSize);
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, WindowIconPath));

        // Windows の設定を変えたら、開いたままでもすぐにライト／ダークを切り替える。
        _themeWatcher = new SystemThemeWatcher();
        RootGrid.RequestedTheme = _themeWatcher.CurrentTheme;
        _themeWatcher.ThemeChanged += OnThemeChanged;
        Closed += (_, _) =>
        {
            _themeWatcher.ThemeChanged -= OnThemeChanged;
            _themeWatcher.Dispose();
        };
    }

    /// <summary>
    /// ウィンドウのハンドルを取得する。Windows Hello の確認画面の親に使う。
    /// </summary>
    public nint Handle => WinRT.Interop.WindowNative.GetWindowHandle(this);

    /// <summary>
    /// ウィンドウの ID を取得する。フォルダー選択画面の親に使う。
    /// </summary>
    public WindowId Id => AppWindow.Id;

    /// <summary>
    /// ウィンドウの中身を差し替える。
    /// </summary>
    /// <param name="content">表示する画面。</param>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> が null。</exception>
    public void ShowContent(UIElement content)
    {
        ArgumentNullException.ThrowIfNull(content);
        ContentHost.Content = content;
    }

    /// <summary>
    /// タイトルバーの右側（最小化ボタンの左）に出す部品を差し替える。
    /// </summary>
    /// <param name="content">出す部品。null なら何も出さない。</param>
    public void ShowTitleBarStatus(UIElement? content) => AppTitleBar.RightHeader = content;

    private void OnThemeChanged(object? sender, ElementTheme theme) => RootGrid.RequestedTheme = theme;
}

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Yamori.ViewModels;

namespace Yamori.Views;

/// <summary>
/// サインイン後のメイン画面。左のナビゲーションで、概要・リアルタイム・ハードウェア・アプリ・設定を切り替える。
/// </summary>
public sealed partial class ShellView : UserControl
{
    private readonly Func<RealtimePageViewModel> _createRealtime;

    /// <summary>
    /// 表示するビューモデルを指定して作る。
    /// </summary>
    /// <param name="main">概要・ハードウェア・アプリの画面の状態。</param>
    /// <param name="settings">設定画面の状態。</param>
    /// <param name="createRealtime">リアルタイム画面の状態を作る処理。画面を開くたびに新しく作る（閉じると測定を止めて破棄するため）。</param>
    /// <param name="openSettings">最初に設定画面を開くなら true（言語を変えて作り直したときなど）。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public ShellView(MainViewModel main, SettingsViewModel settings, Func<RealtimePageViewModel> createRealtime, bool openSettings = false)
    {
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(createRealtime);

        Main = main;
        Settings = settings;
        _createRealtime = createRealtime;
        InitializeComponent();
        Navigation.SelectedItem = openSettings ? SettingsItem : OverviewItem;
    }

    /// <summary>概要・ハードウェア・アプリの画面の状態を取得する。</summary>
    public MainViewModel Main { get; }

    /// <summary>設定画面の状態を取得する。x:Bind の参照先。</summary>
    public SettingsViewModel Settings { get; }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem { Tag: string tag })
        {
            return;
        }

        var page = CreatePage(tag);
        ApplyScrolling(page is not ISelfScrollingPage);
        PageHost.Content = page;
        PageScroller.ChangeView(null, 0, null, disableAnimation: true);
    }

    /// <summary>
    /// 外側のスクロールを切り替える。止めると、画面は窓の高さに収まり、画面の中で自分でスクロールする。
    /// </summary>
    /// <param name="enabled">外側でスクロールするなら true。</param>
    private void ApplyScrolling(bool enabled)
    {
        PageScroller.VerticalScrollMode = enabled ? ScrollMode.Auto : ScrollMode.Disabled;
        PageScroller.VerticalScrollBarVisibility = enabled ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
    }

    private UIElement CreatePage(string tag) =>
        tag switch
        {
            "Realtime" => new RealtimeView(_createRealtime()),
            "Hardware" => new HardwareView(Main),
            "Apps" => new AppsView(Main),
            "Settings" => new SettingsView(Settings),
            _ => new OverviewView(Main),
        };
}

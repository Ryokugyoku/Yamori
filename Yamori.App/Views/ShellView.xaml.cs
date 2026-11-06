using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Yamori.ViewModels;

namespace Yamori.Views;

/// <summary>
/// サインイン後のメイン画面。左のナビゲーションで、概要・ハードウェア・アプリ・設定を切り替える。
/// </summary>
public sealed partial class ShellView : UserControl
{
    /// <summary>
    /// 表示するビューモデルを指定して作る。
    /// </summary>
    /// <param name="main">概要・ハードウェア・アプリの画面の状態。</param>
    /// <param name="settings">設定画面の状態。</param>
    /// <param name="openSettings">最初に設定画面を開くなら true（言語を変えて作り直したときなど）。</param>
    /// <exception cref="ArgumentNullException">いずれかのビューモデルが null。</exception>
    public ShellView(MainViewModel main, SettingsViewModel settings, bool openSettings = false)
    {
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(settings);

        Main = main;
        Settings = settings;
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

        PageHost.Content = CreatePage(tag);
        PageScroller.ChangeView(null, 0, null, disableAnimation: true);
    }

    private UIElement CreatePage(string tag) =>
        tag switch
        {
            "Hardware" => new HardwareView(Main),
            "Apps" => new AppsView(Main),
            "Settings" => new SettingsView(Settings),
            _ => new OverviewView(Main),
        };
}

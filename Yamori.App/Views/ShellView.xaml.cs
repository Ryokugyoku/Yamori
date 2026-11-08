using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Yamori.ViewModels;
using Yamori.ViewModels.Analysis;

namespace Yamori.Views;

/// <summary>
/// サインイン後のメイン画面。左のナビゲーションで、概要・リアルタイム・分析・ハードウェア・アプリ・設定を切り替える。
/// </summary>
public sealed partial class ShellView : UserControl
{
    /// <summary>分析画面のタグ。</summary>
    public const string AnalysisTag = "Analysis";

    private readonly Func<RealtimePageViewModel> _createRealtime;
    private readonly Func<AnalysisViewModel> _createAnalysis;
    private readonly RecentAnomaliesViewModel _recentAnomalies;
    private long? _pendingAnomalyId;

    /// <summary>
    /// 表示するビューモデルを指定して作る。
    /// </summary>
    /// <param name="main">概要・ハードウェア・アプリの画面の状態。</param>
    /// <param name="settings">設定画面の状態。</param>
    /// <param name="createRealtime">リアルタイム画面の状態を作る処理。画面を開くたびに新しく作る（閉じると測定を止めて破棄するため）。</param>
    /// <param name="createAnalysis">分析画面の状態を作る処理。画面を開くたびに新しく作る。</param>
    /// <param name="recentAnomalies">概要の「最近の異常」のカードの状態。カードから分析画面を開く。</param>
    /// <param name="openSettings">最初に設定画面を開くなら true（言語を変えて作り直したときなど）。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public ShellView(
        MainViewModel main,
        SettingsViewModel settings,
        Func<RealtimePageViewModel> createRealtime,
        Func<AnalysisViewModel> createAnalysis,
        RecentAnomaliesViewModel recentAnomalies,
        bool openSettings = false)
    {
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(createRealtime);
        ArgumentNullException.ThrowIfNull(createAnalysis);
        ArgumentNullException.ThrowIfNull(recentAnomalies);

        Main = main;
        Settings = settings;
        _createRealtime = createRealtime;
        _createAnalysis = createAnalysis;
        _recentAnomalies = recentAnomalies;
        InitializeComponent();
        _recentAnomalies.OpenRequested += (_, anomalyId) => OpenAnalysis(anomalyId);
        Navigation.SelectedItem = openSettings ? SettingsItem : OverviewItem;
    }

    /// <summary>概要・ハードウェア・アプリの画面の状態を取得する。</summary>
    public MainViewModel Main { get; }

    /// <summary>設定画面の状態を取得する。x:Bind の参照先。</summary>
    public SettingsViewModel Settings { get; }

    /// <summary>
    /// 指定した画面に切り替える。
    /// </summary>
    /// <param name="tag">画面のタグ（ナビゲーションの項目の Tag。例：<c>Realtime</c>）。</param>
    /// <returns>切り替えたら true。そのタグの項目がなければ false。</returns>
    public bool SelectPage(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        foreach (var item in Navigation.MenuItems.Concat(Navigation.FooterMenuItems))
        {
            if (item is NavigationViewItem { Tag: string itemTag } navigationItem && itemTag == tag)
            {
                Navigation.SelectedItem = navigationItem;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 分析画面を開く。異常を指定したら、その異常を選んだ状態で開く（通知を押したときや、概要のカードから開いたとき）。
    /// すでに分析画面を開いていれば、開き直さずにその異常を選ぶ。
    /// </summary>
    /// <param name="anomalyId">選んでおく異常の番号。なければ null。</param>
    public void OpenAnalysis(long? anomalyId)
    {
        if (PageHost.Content is AnalysisView open)
        {
            if (anomalyId is { } id)
            {
                open.ShowAnomaly(id);
            }

            return;
        }

        _pendingAnomalyId = anomalyId;
        SelectPage(AnalysisTag);
    }

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

    private UIElement CreatePage(string tag)
    {
        switch (tag)
        {
            case "Realtime":
                return new RealtimeView(_createRealtime());
            case AnalysisTag:
                var anomalyId = _pendingAnomalyId;
                _pendingAnomalyId = null;
                return new AnalysisView(_createAnalysis(), anomalyId);
            case "Hardware":
                return new HardwareView(Main);
            case "Apps":
                return new AppsView(Main);
            case "Settings":
                return new SettingsView(Settings);
            default:
                return new OverviewView(Main, _recentAnomalies);
        }
    }
}

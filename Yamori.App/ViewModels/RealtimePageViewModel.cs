using System;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using Yamori.ViewModels.Sensors;

namespace Yamori.ViewModels;

/// <summary>
/// リアルタイム画面の状態。センサーの詳細のツリー（カタログから自動で組み立てる）と、アプリごとの今の使用量を持つ。
/// 左の一覧で選んだもの（ハードウェア 1 つか、アプリ）だけを右に詳しく表示する。
/// </summary>
public sealed class RealtimePageViewModel : ObservableObject, IDisposable
{
    private SensorHardwareItem? _selectedHardware;
    private bool _isAppsSelected;

    /// <summary>
    /// 画面の部品の状態を指定して作る。最初はハードウェアの一覧の先頭を選ぶ。
    /// </summary>
    /// <param name="tree">センサーの詳細のツリー。このインスタンスが破棄する。</param>
    /// <param name="apps">アプリごとの今の使用量。このインスタンスが破棄する。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public RealtimePageViewModel(SensorTreeViewModel tree, RealtimeViewModel apps)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(apps);
        Tree = tree;
        Apps = apps;
        Tree.Hardware.CollectionChanged += OnHardwareChanged;
        EnsureSelection();
    }

    /// <summary>センサーの詳細のツリーを取得する。</summary>
    public SensorTreeViewModel Tree { get; }

    /// <summary>アプリごとの今の使用量を取得する。センサーではないため、従来の測定のまま表示する。</summary>
    public RealtimeViewModel Apps { get; }

    /// <summary>右に詳しく表示しているハードウェアを取得する。アプリを選んでいるとき、まだ何もないときは null。</summary>
    public SensorHardwareItem? SelectedHardware
    {
        get => _selectedHardware;
        private set
        {
            if (SetProperty(ref _selectedHardware, value))
            {
                OnPropertyChanged(nameof(ShowsHardware));
            }
        }
    }

    /// <summary>アプリを選んでいるかどうかを取得する。</summary>
    public bool IsAppsSelected
    {
        get => _isAppsSelected;
        private set
        {
            if (SetProperty(ref _isAppsSelected, value))
            {
                OnPropertyChanged(nameof(ShowsHardware));
            }
        }
    }

    /// <summary>右にハードウェアの詳細を表示するかどうかを取得する。</summary>
    public bool ShowsHardware => !IsAppsSelected && SelectedHardware is not null;

    /// <summary>
    /// ハードウェアを選ぶ。
    /// </summary>
    /// <param name="hardware">選ぶハードウェア。</param>
    /// <exception cref="ArgumentNullException"><paramref name="hardware"/> が null。</exception>
    public void SelectHardware(SensorHardwareItem hardware)
    {
        ArgumentNullException.ThrowIfNull(hardware);

        // 先にアプリの選択を外す（選択の変化を受け取った画面が、アプリを選んだままの状態を見ないように）。
        IsAppsSelected = false;
        SelectedHardware = hardware;
    }

    /// <summary>
    /// アプリを選ぶ。
    /// </summary>
    public void SelectApps()
    {
        IsAppsSelected = true;
        SelectedHardware = null;
    }

    /// <summary>
    /// 画面を開いたときに呼ぶ。アプリごとの使用量の測定を始める。
    /// </summary>
    public void Start() => Apps.Start();

    /// <summary>
    /// 測定を止め、購読を解除する。
    /// </summary>
    public void Dispose()
    {
        Tree.Hardware.CollectionChanged -= OnHardwareChanged;
        Apps.Dispose();
        Tree.Dispose();
    }

    private void OnHardwareChanged(object? sender, NotifyCollectionChangedEventArgs e) => EnsureSelection();

    /// <summary>
    /// 選んでいたハードウェアが一覧から消えたとき、まだ何も選んでいないときは、一覧の先頭を選ぶ。アプリを選んでいるときは変えない。
    /// </summary>
    private void EnsureSelection()
    {
        if (IsAppsSelected || (SelectedHardware is { } selected && Tree.Hardware.Contains(selected)))
        {
            return;
        }

        SelectedHardware = Tree.Hardware.Count > 0 ? Tree.Hardware[0] : null;
    }
}

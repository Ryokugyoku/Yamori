using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Display;
using Yamori.Core.Sensors.Extended;
using Yamori.Presentation.Realtime;
using Yamori.Presentation.Sensors;

namespace Yamori.ViewModels.Sensors;

/// <summary>
/// リアルタイム画面の詳細のツリー（ハードウェア → グループ → センサー）と、拡張センサーの状態。
/// カタログから自動で組み立て、取得元（標準 API か PawnIO か）による分岐はしない。
/// </summary>
public sealed class SensorTreeViewModel : ObservableObject, IDisposable
{
    private readonly SensorBoard _board;
    private string _extendedStateText = string.Empty;
    private InfoBarSeverity _extendedSeverity = InfoBarSeverity.Informational;

    /// <summary>
    /// 共有の状態を指定して作る。UI スレッドから呼ぶこと。
    /// </summary>
    /// <param name="board">センサーの今の状態。</param>
    /// <exception cref="ArgumentNullException"><paramref name="board"/> が null。</exception>
    public SensorTreeViewModel(SensorBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        _board = board;
        _board.Updated += OnUpdated;
        Refresh();
        _ = RefreshExtendedStateAsync();
    }

    /// <summary>ハードウェアごとの表示内容を取得する。</summary>
    public ObservableCollection<SensorHardwareItem> Hardware { get; } = [];

    /// <summary>拡張センサー（PawnIO）の状態の説明を取得する。</summary>
    public string ExtendedStateText
    {
        get => _extendedStateText;
        private set => SetProperty(ref _extendedStateText, value);
    }

    /// <summary>拡張センサーの状態の重さ（表示の色）を取得する。</summary>
    public InfoBarSeverity ExtendedSeverity
    {
        get => _extendedSeverity;
        private set => SetProperty(ref _extendedSeverity, value);
    }

    /// <summary>
    /// 共有の状態の購読を解除する。
    /// </summary>
    public void Dispose() => _board.Updated -= OnUpdated;

    private void OnUpdated(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        var tree = SensorTreeBuilder.Build(_board.Catalog, k => _board.GetState(k) != SensorDisplayState.Hidden);
        KeyedCollectionSync.Sync(Hardware, tree, h => h.Key, d => d.Key, d => FillHardware(new SensorHardwareItem(d.Key), d), (h, d) => FillHardware(h, d));
    }

    private SensorHardwareItem FillHardware(SensorHardwareItem item, SensorTreeHardware data)
    {
        item.Glyph = SensorGlyphs.For(data.Kind);
        item.Title = data.Name;
        item.KindName = SensorTexts.KindName(data.Kind);
        KeyedCollectionSync.Sync(item.Groups, data.Groups, g => g.Key, d => d.Key, d => FillGroup(new SensorGroupItem(d.Key), d), (g, d) => FillGroup(g, d));
        return item;
    }

    private SensorGroupItem FillGroup(SensorGroupItem item, SensorTreeGroup data)
    {
        item.Title = SensorTexts.GroupName(data.Group);
        KeyedCollectionSync.Sync(
            item.Rows,
            data.Sensors,
            r => r.Key,
            d => d.Key.ToString(),
            d => FillRow(new SensorRowItem(d.Key.ToString()), d),
            (r, d) => FillRow(r, d));
        return item;
    }

    private SensorRowItem FillRow(SensorRowItem item, SensorDescriptor descriptor)
    {
        var (text, percent, opacity) = SensorRowFiller.Describe(descriptor, _board.GetLatest(descriptor.Key), _board.GetState(descriptor.Key));
        item.Label = descriptor.DisplayName;
        item.ValueText = text;
        item.Percent = percent ?? 0;
        item.HasPercent = percent is not null;
        item.Opacity = opacity;
        return item;
    }

    private async Task RefreshExtendedStateAsync()
    {
        var state = await _board.Service.GetExtendedSensorStateAsync();
        ExtendedStateText = SensorTexts.ExtendedState(state);
        ExtendedSeverity = state switch
        {
            ExtendedSensorState.Active => InfoBarSeverity.Success,
            ExtendedSensorState.UnsupportedEdition or ExtendedSensorState.Unavailable => InfoBarSeverity.Warning,
            _ => InfoBarSeverity.Informational,
        };
    }
}

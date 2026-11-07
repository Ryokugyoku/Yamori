using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Display;
using Yamori.Core.Sensors.Service;
using Yamori.Presentation.Realtime;
using Yamori.Presentation.Sensors;

namespace Yamori.ViewModels.Sensors;

/// <summary>
/// リアルタイム画面の詳細のツリー（ハードウェア → グループ → センサー）と、画面の上のお知らせ（Helper との接続と拡張センサーの状態）。
/// カタログから自動で組み立て、取得元（標準 API か PawnIO か）による分岐はしない。
/// </summary>
public sealed class SensorTreeViewModel : ObservableObject, IDisposable
{
    private readonly SensorBoard _board;
    private SensorNotice _notice = SensorNotice.Empty;

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
    }

    /// <summary>ハードウェアごとの表示内容を取得する。</summary>
    public ObservableCollection<SensorHardwareItem> Hardware { get; } = [];

    /// <summary>お知らせの見出しを取得する。</summary>
    public string NoticeTitle => _notice.Title;

    /// <summary>お知らせの本文を取得する。</summary>
    public string NoticeText => _notice.Message;

    /// <summary>お知らせの重さ（表示の色）を取得する。</summary>
    public InfoBarSeverity NoticeSeverity => _notice.Severity;

    /// <summary>PawnIO の利用の同意を変えるボタンを出すかどうかを取得する。</summary>
    public bool CanChangeConsent => _notice.ConsentAction is not null;

    /// <summary>PawnIO の利用の同意を変えるボタンの文字列を取得する。</summary>
    public string ConsentActionText => _notice.ConsentAction is { } enable ? SensorTexts.ConsentAction(enable) : string.Empty;

    /// <summary>ボタンを押したときに、同意する（true）か取り消す（false）かを取得する。ボタンを出さないときは null。</summary>
    public bool? ConsentAction => _notice.ConsentAction;

    /// <summary>
    /// PawnIO の利用の同意を変える。
    /// </summary>
    /// <param name="enabled">同意するなら true。</param>
    /// <returns>変えられなかった場合は、その理由の説明。変えられた場合は null。</returns>
    public async Task<string?> ApplyConsentAsync(bool enabled)
    {
        var result = await _board.Service.SetPawnIoConsentAsync(enabled);
        return result == SensorSettingResult.Applied ? null : SensorTexts.ConsentResult(result);
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

        var notice = SensorNotice.Describe(_board.ConnectionState, _board.ExtendedState);
        if (notice != _notice)
        {
            _notice = notice;
            OnPropertyChanged(nameof(NoticeTitle));
            OnPropertyChanged(nameof(NoticeText));
            OnPropertyChanged(nameof(NoticeSeverity));
            OnPropertyChanged(nameof(CanChangeConsent));
            OnPropertyChanged(nameof(ConsentActionText));
            OnPropertyChanged(nameof(ConsentAction));
        }
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
}

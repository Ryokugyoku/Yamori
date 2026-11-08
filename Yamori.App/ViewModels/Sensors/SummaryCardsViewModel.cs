using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Display;
using Yamori.Presentation.Realtime;
using Yamori.Presentation.Sensors;

namespace Yamori.ViewModels.Sensors;

/// <summary>
/// 概要画面のサマリーカード。カードごとに候補のキーから表示するセンサーを選び、どの候補も使えないカードは表示しない。
/// </summary>
public sealed class SummaryCardsViewModel : IDisposable
{
    private readonly SensorBoard _board;
    private readonly IReadOnlyList<SummaryCardDefinition> _definitions;

    /// <summary>
    /// 共有の状態とカードの定義を指定して作る。UI スレッドから呼ぶこと。
    /// </summary>
    /// <param name="board">センサーの今の状態。</param>
    /// <param name="definitions">カードの定義（表示する順）。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public SummaryCardsViewModel(SensorBoard board, IReadOnlyList<SummaryCardDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(definitions);
        _board = board;
        _definitions = definitions;
        _board.Updated += OnUpdated;
        Refresh();
    }

    /// <summary>表示するカードを取得する。</summary>
    public ObservableCollection<SummaryCardItem> Cards { get; } = [];

    /// <summary>
    /// 表示を作り直す（表示言語を変えたときなど）。
    /// </summary>
    public void Refresh()
    {
        var visible = _board.VisibleKeys();
        var data = new List<(string Id, SensorKey Key, SensorDescriptor Descriptor)>();
        foreach (var definition in _definitions)
        {
            if (SummaryCardResolver.Resolve(definition, visible) is { } key && _board.GetDescriptor(key) is { } descriptor)
            {
                data.Add((definition.Id, key, descriptor));
            }
        }

        KeyedCollectionSync.Sync(
            Cards,
            data,
            c => c.Id,
            d => d.Id,
            d =>
            {
                var item = new SummaryCardItem(d.Id);
                Fill(item, d.Key, d.Descriptor);
                return item;
            },
            (c, d) => Fill(c, d.Key, d.Descriptor));
    }

    /// <summary>
    /// 共有の状態の購読を解除する。
    /// </summary>
    public void Dispose() => _board.Updated -= OnUpdated;

    private void OnUpdated(object? sender, EventArgs e) => Refresh();

    private void Fill(SummaryCardItem item, SensorKey key, SensorDescriptor descriptor)
    {
        item.Glyph = SensorGlyphs.ForCard(key);
        item.Title = SensorTexts.CardTitle(item.Id);
        item.Source = descriptor.HardwareName;
        SensorCardFiller.Fill(item, _board, descriptor);
    }
}

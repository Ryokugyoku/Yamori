using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Yamori.ViewModels.Sensors;

/// <summary>
/// サマリーカード 1 枚の表示内容。更新では作り直さず、中身を書き換える。
/// </summary>
public sealed class SummaryCardItem : ObservableObject
{
    private string _glyph = string.Empty;
    private string _title = string.Empty;
    private string _source = string.Empty;
    private string _valueText = string.Empty;
    private double _percent;
    private bool _hasPercent;
    private double _opacity = 1;
    private IReadOnlyList<double> _trend = [];
    private double _trendMinimum = double.NaN;
    private double _trendMaximum = double.NaN;

    /// <summary>
    /// カードの ID を指定して作る。
    /// </summary>
    /// <param name="id">カードの ID。</param>
    public SummaryCardItem(string id) => Id = id;

    /// <summary>カードの ID を取得する。</summary>
    public string Id { get; }

    /// <summary>アイコンを取得する。</summary>
    public string Glyph { get => _glyph; set => SetProperty(ref _glyph, value); }

    /// <summary>見出しを取得する。</summary>
    public string Title { get => _title; set => SetProperty(ref _title, value); }

    /// <summary>表示しているセンサーの名前（どのハードウェアのどの値か）を取得する。</summary>
    public string Source { get => _source; set => SetProperty(ref _source, value); }

    /// <summary>値の表示を取得する。読めないときは「—」。</summary>
    public string ValueText { get => _valueText; set => SetProperty(ref _valueText, value); }

    /// <summary>棒グラフで示す割合（0〜100）を取得する。</summary>
    public double Percent { get => _percent; set => SetProperty(ref _percent, value); }

    /// <summary>棒グラフを出すかどうかを取得する。</summary>
    public bool HasPercent { get => _hasPercent; set => SetProperty(ref _hasPercent, value); }

    /// <summary>不透明度を取得する。一時的に読めないときは薄くする。</summary>
    public double Opacity { get => _opacity; set => SetProperty(ref _opacity, value); }

    /// <summary>直近の値（古い順）を取得する。折れ線グラフに使う。</summary>
    public IReadOnlyList<double> Trend { get => _trend; set => SetProperty(ref _trend, value); }

    /// <summary>折れ線グラフの縦軸の下限を取得する。NaN なら値から決める。</summary>
    public double TrendMinimum { get => _trendMinimum; set => SetProperty(ref _trendMinimum, value); }

    /// <summary>折れ線グラフの縦軸の上限を取得する。NaN なら値から決める。</summary>
    public double TrendMaximum { get => _trendMaximum; set => SetProperty(ref _trendMaximum, value); }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Yamori.ViewModels.Sensors;

/// <summary>
/// 詳細のツリーの 1 段目（ハードウェア）の表示内容。リアルタイム画面の左の一覧の 1 行と、選んだときの右の詳細になる。
/// </summary>
public sealed class SensorHardwareItem : ObservableObject
{
    private string _glyph = string.Empty;
    private string _title = string.Empty;
    private string _kindName = string.Empty;
    private string _headlineText = string.Empty;
    private double _headlinePercent;
    private bool _hasHeadlinePercent;
    private bool _hasHighlights;

    /// <summary>
    /// ハードウェアのキーを指定して作る。
    /// </summary>
    /// <param name="key">ハードウェアを識別するキー。</param>
    public SensorHardwareItem(string key) => Key = key;

    /// <summary>ハードウェアを識別するキーを取得する。</summary>
    public string Key { get; }

    /// <summary>アイコンを取得する。</summary>
    public string Glyph { get => _glyph; set => SetProperty(ref _glyph, value); }

    /// <summary>ハードウェアの名前を取得する。</summary>
    public string Title { get => _title; set => SetProperty(ref _title, value); }

    /// <summary>ハードウェアの種類の名前を取得する。</summary>
    public string KindName { get => _kindName; set => SetProperty(ref _kindName, value); }

    /// <summary>左の一覧に出す、いちばん大事な値（主な値の 1 つ目）を取得する。なければ空。</summary>
    public string HeadlineText { get => _headlineText; set => SetProperty(ref _headlineText, value); }

    /// <summary>左の一覧の棒グラフで示す割合（0〜100）を取得する。</summary>
    public double HeadlinePercent { get => _headlinePercent; set => SetProperty(ref _headlinePercent, value); }

    /// <summary>左の一覧に棒グラフを出すかどうかを取得する。</summary>
    public bool HasHeadlinePercent { get => _hasHeadlinePercent; set => SetProperty(ref _hasHeadlinePercent, value); }

    /// <summary>主な値が 1 つ以上あるかどうかを取得する。</summary>
    public bool HasHighlights { get => _hasHighlights; set => SetProperty(ref _hasHighlights, value); }

    /// <summary>詳細の上に大きく見せる主な値を取得する。</summary>
    public ObservableCollection<SummaryCardItem> Highlights { get; } = [];

    /// <summary>詳細のカード（グループ。種類が混ざるグループは種類ごとに分けたもの）を取得する。</summary>
    public ObservableCollection<SensorGroupItem> Groups { get; } = [];

    /// <summary>
    /// 種類の名前とハードウェアの名前を返す。一覧の行の読み上げの名前（UI オートメーションの名前）に使われる。
    /// </summary>
    /// <returns>例：<c>GPU NVIDIA GeForce RTX 3090 Ti</c>。</returns>
    public override string ToString() => KindName + " " + Title;
}

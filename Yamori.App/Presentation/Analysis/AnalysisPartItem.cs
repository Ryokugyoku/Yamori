using CommunityToolkit.Mvvm.ComponentModel;
using Yamori.Core.Analysis.Parts;

namespace Yamori.Presentation.Analysis;

/// <summary>
/// 分析画面の左の一覧の 1 行（「すべての異常」か、部品の種類 1 つ）。異常の数は期間を変えると更新する。
/// </summary>
public sealed class AnalysisPartItem : ObservableObject
{
    private int _count;
    private string _countText = string.Empty;

    /// <summary>
    /// 行を作る。
    /// </summary>
    /// <param name="part">部品の種類。「すべての異常」なら null。</param>
    /// <param name="name">表示名。</param>
    /// <param name="glyph">アイコンの文字。</param>
    public AnalysisPartItem(AnalysisPart? part, string name, string glyph)
    {
        Part = part;
        Name = name;
        Glyph = glyph;
    }

    /// <summary>部品の種類を取得する。「すべての異常」なら null。</summary>
    public AnalysisPart? Part { get; }

    /// <summary>「すべての異常」の行かどうかを取得する。</summary>
    public bool IsAll => Part is null;

    /// <summary>表示名を取得する。</summary>
    public string Name { get; }

    /// <summary>アイコンの文字を取得する。</summary>
    public string Glyph { get; }

    /// <summary>期間の中の異常の数（「これは正常」としたものを除く）を取得する。</summary>
    public int Count
    {
        get => _count;
        private set
        {
            if (SetProperty(ref _count, value))
            {
                OnPropertyChanged(nameof(HasCount));
            }
        }
    }

    /// <summary>数があるかどうか（札を出すか）を取得する。</summary>
    public bool HasCount => _count > 0;

    /// <summary>数の札の文字（読み上げ用の説明つき）を取得する。</summary>
    public string CountText
    {
        get => _countText;
        private set => SetProperty(ref _countText, value);
    }

    /// <summary>
    /// 異常の数を変える。
    /// </summary>
    /// <param name="count">数。</param>
    /// <param name="countText">数の札の文字。</param>
    public void SetCount(int count, string countText)
    {
        Count = count;
        CountText = countText;
    }
}

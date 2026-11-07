using System.Collections.Generic;

namespace Yamori.Presentation.Realtime;

/// <summary>
/// リアルタイム画面の項目を順に集める。値が分からない項目は加えない（「不明」を並べて画面を埋めないため）。
/// </summary>
public sealed class RealtimeFactList
{
    private readonly List<RealtimeFactData> _facts = [];

    /// <summary>
    /// 項目名を直接指定して加える。値が null か空の場合は何もしない。
    /// </summary>
    /// <param name="key">項目のキー。同じ一覧の中で重複しないこと。</param>
    /// <param name="label">項目名。</param>
    /// <param name="value">値の表示。</param>
    /// <param name="percent">棒グラフで示す割合（0〜100）。出さない場合は null。</param>
    /// <returns>このインスタンス。</returns>
    public RealtimeFactList AddLabeled(string key, string label, string? value, double? percent = null)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _facts.Add(new RealtimeFactData(key, label, value, percent));
        }

        return this;
    }

    /// <summary>
    /// 集めた項目を取得する。
    /// </summary>
    /// <returns>加えた順の項目の一覧。</returns>
    public IReadOnlyList<RealtimeFactData> ToList() => _facts.ToArray();
}

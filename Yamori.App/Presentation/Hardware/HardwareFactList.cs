using System.Collections.Generic;
using Yamori.Localization;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// 部品の情報の項目を順に集める。値が分からない項目は加えない（「不明」を並べて画面を埋めないため）。
/// </summary>
public sealed class HardwareFactList
{
    private readonly List<HardwareFact> _facts = [];

    /// <summary>
    /// 項目を加える。値が null か空の場合は何もしない。
    /// </summary>
    /// <param name="labelKey">項目名のリソースのキー。</param>
    /// <param name="value">値。</param>
    /// <returns>このインスタンス。</returns>
    public HardwareFactList Add(string labelKey, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _facts.Add(new HardwareFact(UiText.Get(labelKey), value));
        }

        return this;
    }

    /// <summary>
    /// 集めた項目を取得する。
    /// </summary>
    /// <returns>加えた順の項目の一覧。</returns>
    public IReadOnlyList<HardwareFact> ToList() => _facts.ToArray();
}

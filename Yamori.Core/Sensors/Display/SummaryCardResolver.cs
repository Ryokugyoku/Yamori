namespace Yamori.Core.Sensors.Display;

/// <summary>
/// サマリーカードに表示するセンサーを、候補のキーから選ぶ。
/// </summary>
public static class SummaryCardResolver
{
    /// <summary>
    /// カードに表示するセンサーを選ぶ。候補を優先する順に見て、表示できるキーに一致した最初の候補を使う。
    /// 1 つの候補に複数のキーが一致する場合（GPU が 2 枚あるなど）は、キーの文字列の順で最初のものを使う。
    /// </summary>
    /// <param name="definition">カードの定義。</param>
    /// <param name="visibleKeys">今表示できるセンサーのキー。</param>
    /// <returns>表示するセンサーのキー。どの候補も使えなければ null（カードごと表示しない）。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static SensorKey? Resolve(SummaryCardDefinition definition, IEnumerable<SensorKey> visibleKeys)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(visibleKeys);

        var ordered = visibleKeys.OrderBy(k => k.ToString(), StringComparer.Ordinal).ToArray();
        foreach (var candidate in definition.Candidates)
        {
            foreach (var key in ordered)
            {
                if (candidate.Matches(key))
                {
                    return key;
                }
            }
        }

        return null;
    }
}

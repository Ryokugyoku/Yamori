namespace Yamori.Core.Sensors.Display;

/// <summary>
/// サマリー（固定枠）のカード 1 枚の定義。候補のキーを優先する順に持ち、使えるもののうち最初のものを表示する。
/// </summary>
/// <param name="Id">カードの ID（例：<c>CpuTemperature</c>）。表示名などは UI がこの ID から決める。</param>
/// <param name="Candidates">候補のキーのパターンを、優先する順に並べたもの。</param>
public sealed record SummaryCardDefinition(string Id, IReadOnlyList<SensorKeyPattern> Candidates)
{
    /// <summary>
    /// パターンの表記からカードの定義を作る。
    /// </summary>
    /// <param name="id">カードの ID。</param>
    /// <param name="candidates">候補のキーのパターンの表記を、優先する順に並べたもの。</param>
    /// <returns>新しい定義。</returns>
    /// <exception cref="FormatException">パターンの表記が正しくない。</exception>
    public static SummaryCardDefinition Create(string id, params string[] candidates) =>
        new(id, candidates.Select(SensorKeyPattern.Parse).ToArray());
}

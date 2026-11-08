namespace Yamori.Core.Sensors.Display;

/// <summary>
/// リアルタイム画面で、ハードウェアごとに大きく見せる値 1 つ分の枠。候補のうち、最初に見つかったセンサーを表示する。
/// </summary>
/// <param name="Candidates">
/// 候補を優先する順に並べたもの。各候補は量の種類と名前を <c>/</c> でつないだ表記（例：<c>load/total</c>）。
/// </param>
public sealed record SensorHighlightSlot(IReadOnlyList<string> Candidates)
{
    /// <summary>
    /// 候補を指定して作る。
    /// </summary>
    /// <param name="candidates">候補を優先する順に並べたもの。</param>
    /// <returns>新しい枠。</returns>
    public static SensorHighlightSlot Of(params string[] candidates) => new(candidates);

    /// <summary>
    /// センサーがいずれかの候補に一致するかどうかを調べ、一致した候補の順位を返す。
    /// </summary>
    /// <param name="key">調べるセンサーのキー。</param>
    /// <returns>一致した候補の順位（0 が最優先）。一致しなければ -1。</returns>
    public int RankOf(SensorKey key)
    {
        var token = SensorKeyTokens.ToToken(key.SensorType) + SensorKeyTokens.Separator + key.Name;
        for (var i = 0; i < Candidates.Count; i++)
        {
            if (string.Equals(Candidates[i], token, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}

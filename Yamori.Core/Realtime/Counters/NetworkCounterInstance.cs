namespace Yamori.Core.Realtime.Counters;

/// <summary>
/// <c>\Network Interface(*)</c> のインスタンス名と、ネットワーク アダプターの説明を結び付ける。
/// </summary>
public static class NetworkCounterInstance
{
    /// <summary>
    /// アダプターの説明を、パフォーマンスカウンターのインスタンス名と同じ形にする。
    /// Windows はインスタンス名に使えない文字を置き換える（<c>(</c> → <c>[</c>、<c>)</c> → <c>]</c>、<c>#</c>・<c>/</c>・<c>\</c> → <c>_</c>）。
    /// </summary>
    /// <param name="description">アダプターの説明（例：<c>Intel(R) Ethernet Controller (3) I225-V</c>）。</param>
    /// <returns>インスタンス名の形にした文字列（例：<c>Intel[R] Ethernet Controller [3] I225-V</c>）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="description"/> が null。</exception>
    public static string FromDescription(string description)
    {
        ArgumentNullException.ThrowIfNull(description);

        return string.Create(description.Length, description, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                span[i] = source[i] switch
                {
                    '(' => '[',
                    ')' => ']',
                    '#' or '/' or '\\' => '_',
                    var c => c,
                };
            }
        });
    }
}

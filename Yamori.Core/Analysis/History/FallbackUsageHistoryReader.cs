using Yamori.Core.Hardware;

namespace Yamori.Core.Analysis.History;

/// <summary>
/// 先に指定した読み取り元から順に試し、最初に履歴が見つかったものを使う。
/// </summary>
/// <remarks>
/// 記録元の違う履歴は混ぜない（同じ時間を二重に数えないため）。
/// 例：常駐サービスの時系列を先に、画面の CSV を後にすると、Helper が記録していればそれを、記録がなければ CSV を使う。
/// </remarks>
public sealed class FallbackUsageHistoryReader : IUsageHistoryReader
{
    private readonly IReadOnlyList<IUsageHistoryReader> _readers;

    /// <summary>
    /// 試す順に読み取り元を指定して作る。
    /// </summary>
    /// <param name="readers">読み取り元（先にあるものを優先する）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="readers"/> が null、または null を含む。</exception>
    public FallbackUsageHistoryReader(params IReadOnlyList<IUsageHistoryReader> readers)
    {
        ArgumentNullException.ThrowIfNull(readers);
        foreach (var reader in readers)
        {
            ArgumentNullException.ThrowIfNull(reader, nameof(readers));
        }

        _readers = readers.ToArray();
    }

    /// <inheritdoc />
    public async Task<UsageHistory> ReadAsync(
        HardwareComponent component,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        foreach (var reader in _readers)
        {
            var history = await reader.ReadAsync(component, from, to, cancellationToken).ConfigureAwait(false);
            if (!history.IsEmpty)
            {
                return history;
            }
        }

        return UsageHistory.Empty;
    }
}

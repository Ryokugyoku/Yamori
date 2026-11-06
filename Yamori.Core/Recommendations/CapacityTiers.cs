namespace Yamori.Core.Recommendations;

/// <summary>
/// 市販の PC でよく見る容量・コア数の段階。おすすめの数値を、実際に買える構成に丸めるために使う。
/// </summary>
public static class CapacityTiers
{
    /// <summary>メモリ容量の段階（GB）。</summary>
    public static readonly IReadOnlyList<int> MemoryGigabytes = [4, 8, 16, 32, 64, 128];

    /// <summary>CPU の論理プロセッサ数（スレッド数）の段階。</summary>
    public static readonly IReadOnlyList<int> LogicalProcessors = [4, 8, 12, 16, 20, 24, 32, 64];

    /// <summary>
    /// 指定した値以上で、最も小さい段階を返す。
    /// </summary>
    /// <param name="tiers">昇順に並んだ段階。</param>
    /// <param name="value">基準の値。</param>
    /// <returns>該当する段階。すべての段階を超える場合は最大の段階。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tiers"/> が null。</exception>
    /// <exception cref="ArgumentException"><paramref name="tiers"/> が空。</exception>
    public static int AtLeast(IReadOnlyList<int> tiers, int value)
    {
        EnsureNotEmpty(tiers);
        return tiers.FirstOrDefault(t => t >= value, tiers[^1]);
    }

    /// <summary>
    /// 指定した値より大きい、最も小さい段階を返す。
    /// </summary>
    /// <param name="tiers">昇順に並んだ段階。</param>
    /// <param name="value">基準の値。</param>
    /// <returns>該当する段階。すべての段階以上の場合は最大の段階。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tiers"/> が null。</exception>
    /// <exception cref="ArgumentException"><paramref name="tiers"/> が空。</exception>
    public static int Above(IReadOnlyList<int> tiers, int value)
    {
        EnsureNotEmpty(tiers);
        return tiers.FirstOrDefault(t => t > value, tiers[^1]);
    }

    private static void EnsureNotEmpty(IReadOnlyList<int> tiers)
    {
        ArgumentNullException.ThrowIfNull(tiers);
        if (tiers.Count == 0)
        {
            throw new ArgumentException("段階が 1 つ以上必要です。", nameof(tiers));
        }
    }
}

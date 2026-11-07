namespace Yamori.Core.Realtime.Counters;

/// <summary>
/// Windows のパフォーマンスカウンターを、まとめて取り込んでから読み出す。
/// </summary>
/// <remarks>
/// 1 秒あたりの回数や使用率のような値は、前回の取り込みとの差から計算されるため、
/// 初めて読んだカウンターの値は、次に <see cref="Collect"/> を呼んだ後に分かるようになる。スレッドセーフではない。
/// </remarks>
public interface IPerformanceCounterSampler : IDisposable
{
    /// <summary>
    /// これまでに読んだすべてのカウンターの今の値を取り込む。
    /// </summary>
    void Collect();

    /// <summary>
    /// カウンターの値を、インスタンスごとに読み出す。初めて指定したカウンターは、ここで取り込みの対象に加える。
    /// </summary>
    /// <param name="counterPath">英語名のカウンターのパス（例：<c>\PhysicalDisk(*)\% Idle Time</c>）。</param>
    /// <returns>インスタンス名をキーにした値。インスタンスのないカウンターではキーが空文字列。読めない場合は空。</returns>
    IReadOnlyDictionary<string, double> Read(string counterPath);

    /// <summary>
    /// カウンターがこの PC にあるかどうかを調べる。あれば取り込みの対象に加える。
    /// </summary>
    /// <param name="counterPath">英語名のカウンターのパス。</param>
    /// <returns>カウンターがあれば true。値をまだ計算できていなくても true になる。</returns>
    bool IsAvailable(string counterPath);
}

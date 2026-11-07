namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// MSR を読む（読み取りだけ）。実装は許可リスト（<see cref="IntelMsrRegisters.AllowedReads"/>）にない MSR を読まないこと。
/// </summary>
public interface IMsrReader : IDisposable
{
    /// <summary>
    /// 指定したコアで、いくつかの MSR を続けて読む。
    /// </summary>
    /// <param name="core">読むコア。パッケージ全体の MSR なら null（どの論理プロセッサで読んでもよい）。</param>
    /// <param name="registers">読む MSR の番号。</param>
    /// <param name="values">読んだ値を受け取る。<paramref name="registers"/> と同じ長さにすること。</param>
    /// <returns>すべて読めたら true。1 つでも読めなかった、または許可リストにない MSR があれば false。</returns>
    bool TryRead(CpuCore? core, ReadOnlySpan<uint> registers, Span<ulong> values);
}

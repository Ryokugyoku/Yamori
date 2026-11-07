namespace Yamori.Core.Processes;

/// <summary>
/// アプリ単位（同じ名前のプロセスをまとめた単位）の、ある時点の CPU・GPU・入出力の使用量。
/// </summary>
/// <param name="Name">アプリのプロセス名（例：chrome）。</param>
/// <param name="ProcessCount">まとめたプロセスの数。</param>
/// <param name="CpuPercent">CPU 使用率の合計（PC 全体を 100 とする %）。</param>
/// <param name="GpuPercent">GPU 使用率の合計（%。100 を上限とする）。</param>
/// <param name="ReadBytesPerSecond">読み込みの速さの合計（バイト／秒）。</param>
/// <param name="WriteBytesPerSecond">書き込みの速さの合計（バイト／秒）。</param>
public sealed record AppActivity(
    string Name,
    int ProcessCount,
    double CpuPercent,
    double GpuPercent,
    double ReadBytesPerSecond,
    double WriteBytesPerSecond)
{
    /// <summary>読み書きの速さの合計（バイト／秒）を取得する。</summary>
    public double IoBytesPerSecond => ReadBytesPerSecond + WriteBytesPerSecond;
}

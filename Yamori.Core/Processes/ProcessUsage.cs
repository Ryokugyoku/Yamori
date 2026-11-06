namespace Yamori.Core.Processes;

/// <summary>
/// アプリ単位（同じ名前のプロセスをまとめた単位）の資源使用量。
/// </summary>
/// <param name="Name">アプリのプロセス名（例：chrome）。</param>
/// <param name="ProcessCount">まとめたプロセスの数。</param>
/// <param name="WorkingSetBytes">物理メモリ使用量の合計（バイト）。</param>
public sealed record ProcessUsage(string Name, int ProcessCount, ulong WorkingSetBytes);

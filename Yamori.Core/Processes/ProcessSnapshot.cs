namespace Yamori.Core.Processes;

/// <summary>
/// ある時点での、1 つのプロセスの資源使用量。
/// </summary>
/// <param name="ProcessId">プロセス ID。</param>
/// <param name="Name">プロセス名（拡張子なし、例：chrome）。</param>
/// <param name="WorkingSetBytes">物理メモリの使用量（ワーキングセット、バイト）。</param>
public readonly record struct ProcessSnapshot(int ProcessId, string Name, ulong WorkingSetBytes);

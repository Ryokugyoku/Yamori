namespace Yamori.Core.Processes;

/// <summary>
/// ある時点での、1 つのプロセスの CPU・GPU・入出力の使用量。
/// </summary>
/// <param name="ProcessId">プロセス ID。</param>
/// <param name="Name">プロセス名（拡張子なし、例：chrome）。</param>
/// <param name="CpuPercent">CPU 使用率（PC 全体を 100 とする %）。</param>
/// <param name="GpuPercent">GPU 使用率（最も使っているエンジンの %）。</param>
/// <param name="ReadBytesPerSecond">読み込みの速さ（バイト／秒）。ファイル・デバイス・ネットワークへの入出力をすべて含む。</param>
/// <param name="WriteBytesPerSecond">書き込みの速さ（バイト／秒）。同上。</param>
public readonly record struct ProcessActivity(
    int ProcessId,
    string Name,
    double CpuPercent,
    double GpuPercent,
    double ReadBytesPerSecond,
    double WriteBytesPerSecond);

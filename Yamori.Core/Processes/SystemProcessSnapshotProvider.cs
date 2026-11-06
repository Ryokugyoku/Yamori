using System.ComponentModel;
using System.Diagnostics;

namespace Yamori.Core.Processes;

/// <summary>
/// <see cref="Process"/> を使って、実行中のプロセスの資源使用量を取得する。
/// </summary>
/// <remarks>
/// 管理者権限なしで読めない情報（他ユーザーのプロセスの一部など）は、そのプロセスを飛ばす。
/// </remarks>
public sealed class SystemProcessSnapshotProvider : IProcessSnapshotProvider
{
    /// <inheritdoc />
    public IReadOnlyList<ProcessSnapshot> Capture()
    {
        var processes = Process.GetProcesses();
        var snapshots = new List<ProcessSnapshot>(processes.Length);
        foreach (var process in processes)
        {
            using (process)
            {
                try
                {
                    snapshots.Add(new ProcessSnapshot(process.Id, process.ProcessName, (ulong)Math.Max(0, process.WorkingSet64)));
                }
                catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
                {
                    // 取得中に終了したプロセスや、権限がなく読めないプロセスは集計しない。
                }
            }
        }

        return snapshots;
    }
}

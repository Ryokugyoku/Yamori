using System.Runtime.InteropServices;
using Yamori.Core.Interop;

namespace Yamori.Core.Realtime.Sources;

/// <summary>
/// GetPerformanceInfo と GlobalMemoryStatusEx から、Windows 全体のメモリの使い方を読み取る。管理者権限は必要ない。
/// </summary>
public sealed class Win32SystemPerformanceSource : ISystemPerformanceSource
{
    /// <inheritdoc />
    public SystemPerformance? Read()
    {
        var memory = NativeMethods.MemoryStatusEx.Create();
        if (!NativeMethods.GlobalMemoryStatusEx(ref memory)
            || !NativeMethods.GetPerformanceInfo(out var info, (uint)Marshal.SizeOf<NativeMethods.PerformanceInformation>()))
        {
            return null;
        }

        var page = (ulong)info.PageSize;
        return new SystemPerformance(
            TotalPhysicalBytes: memory.TotalPhys,
            AvailablePhysicalBytes: memory.AvailPhys,
            MemoryLoadPercent: memory.TotalPhys == 0 ? 0 : 100.0 * (memory.TotalPhys - memory.AvailPhys) / memory.TotalPhys,
            CommittedBytes: (ulong)info.CommitTotal * page,
            CommitLimitBytes: (ulong)info.CommitLimit * page,
            CommitPeakBytes: (ulong)info.CommitPeak * page,
            SystemCacheBytes: (ulong)info.SystemCache * page,
            KernelPagedBytes: (ulong)info.KernelPaged * page,
            KernelNonpagedBytes: (ulong)info.KernelNonpaged * page,
            ProcessCount: (int)info.ProcessCount,
            ThreadCount: (int)info.ThreadCount,
            HandleCount: (int)info.HandleCount,
            Uptime: TimeSpan.FromMilliseconds(Environment.TickCount64));
    }
}

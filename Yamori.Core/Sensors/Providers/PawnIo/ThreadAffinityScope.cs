using System.Runtime.InteropServices;

namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// 今のスレッドを 1 つの論理プロセッサに固定し、破棄したら元に戻す。コアごとの MSR を、そのコアで読むために使う。
/// </summary>
/// <remarks>固定している間に <c>await</c> しないこと（ほかのスレッドで続きが動くと、固定が外れない）。</remarks>
public sealed partial class ThreadAffinityScope : IDisposable
{
    private readonly nint _thread;
    private readonly GroupAffinity _previous;
    private bool _disposed;

    private ThreadAffinityScope(nint thread, GroupAffinity previous)
    {
        _thread = thread;
        _previous = previous;
    }

    /// <summary>
    /// 今のスレッドを指定した論理プロセッサに固定する。
    /// </summary>
    /// <param name="group">プロセッサ グループ。</param>
    /// <param name="logicalProcessor">グループの中の論理プロセッサの番号（0〜63）。</param>
    /// <returns>固定した範囲。固定できなければ null。</returns>
    public static ThreadAffinityScope? TryPin(ushort group, byte logicalProcessor)
    {
        if (logicalProcessor >= 64)
        {
            return null;
        }

        var thread = GetCurrentThread();
        var target = new GroupAffinity { Mask = 1UL << logicalProcessor, Group = group };
        if (!SetThreadGroupAffinity(thread, in target, out var previous))
        {
            return null;
        }

        // 固定した直後は、まだ別の論理プロセッサで動いていることがある。いったん譲って移動させる。
        Thread.Yield();
        return new ThreadAffinityScope(thread, previous);
    }

    /// <summary>
    /// 固定を元に戻す。
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            SetThreadGroupAffinity(_thread, in _previous, out _);
        }
    }

    [LibraryImport("kernel32.dll")]
    private static partial nint GetCurrentThread();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetThreadGroupAffinity(nint thread, in GroupAffinity groupAffinity, out GroupAffinity previousGroupAffinity);

    /// <summary>GROUP_AFFINITY。</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct GroupAffinity
    {
        public ulong Mask;
        public ushort Group;
        public ushort Reserved0;
        public ushort Reserved1;
        public ushort Reserved2;
    }
}

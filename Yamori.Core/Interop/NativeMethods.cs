using System.Runtime.InteropServices;

namespace Yamori.Core.Interop;

/// <summary>
/// Yamori が使う Win32 API の宣言。いずれも管理者権限を必要としない。
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>
    /// システム全体の CPU 時間の累計値を取得する。
    /// </summary>
    /// <param name="idleTime">アイドル時間（100 ナノ秒単位）。</param>
    /// <param name="kernelTime">カーネルモード時間（アイドルを含む、100 ナノ秒単位）。</param>
    /// <param name="userTime">ユーザーモード時間（100 ナノ秒単位）。</param>
    /// <returns>成功した場合は true。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetSystemTimes(out long idleTime, out long kernelTime, out long userTime);

    /// <summary>
    /// 物理メモリと仮想メモリの使用状況を取得する。
    /// </summary>
    /// <param name="buffer">結果を受け取る構造体。<see cref="MemoryStatusEx.Length"/> を設定してから渡す。</param>
    /// <returns>成功した場合は true。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    /// <summary>
    /// Win32 の MEMORYSTATUSEX 構造体。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryStatusEx
    {
        /// <summary>構造体のサイズ（バイト）。</summary>
        public uint Length;

        /// <summary>物理メモリの使用率（0〜100）。</summary>
        public uint MemoryLoad;

        /// <summary>物理メモリの合計（バイト）。</summary>
        public ulong TotalPhys;

        /// <summary>使用可能な物理メモリ（バイト）。</summary>
        public ulong AvailPhys;

        /// <summary>コミット上限（バイト）。</summary>
        public ulong TotalPageFile;

        /// <summary>使用可能なコミット量（バイト）。</summary>
        public ulong AvailPageFile;

        /// <summary>仮想アドレス空間の合計（バイト）。</summary>
        public ulong TotalVirtual;

        /// <summary>使用可能な仮想アドレス空間（バイト）。</summary>
        public ulong AvailVirtual;

        /// <summary>予約済み。常に 0。</summary>
        public ulong AvailExtendedVirtual;

        /// <summary>
        /// <see cref="Length"/> を設定済みの構造体を作る。
        /// </summary>
        /// <returns>API に渡せる状態の構造体。</returns>
        public static MemoryStatusEx Create() =>
            new() { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
    }
}

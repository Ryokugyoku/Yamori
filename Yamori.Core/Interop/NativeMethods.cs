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
    /// ファームウェアのテーブル（SMBIOS など）を取得する。
    /// </summary>
    /// <param name="firmwareTableProviderSignature">テーブルの提供元を表す署名（SMBIOS は 'RSMB'）。</param>
    /// <param name="firmwareTableId">テーブルの識別子。SMBIOS は 0。</param>
    /// <param name="firmwareTableBuffer">結果を受け取るバッファー。null の場合は必要なサイズだけを返す。</param>
    /// <param name="bufferSize">バッファーのサイズ（バイト）。</param>
    /// <returns>書き込んだ、または必要なサイズ（バイト）。失敗した場合は 0。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    internal static partial uint GetSystemFirmwareTable(
        uint firmwareTableProviderSignature,
        uint firmwareTableId,
        [Out] byte[]? firmwareTableBuffer,
        uint bufferSize);

    /// <summary>
    /// 電源の状態（AC 電源につながっているか、など）を取得する。
    /// </summary>
    /// <param name="status">結果を受け取る構造体。</param>
    /// <returns>成功した場合は true。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetSystemPowerStatus(out SystemPowerStatus status);

    /// <summary>
    /// 論理プロセッサとコアの関係を取得する。
    /// </summary>
    /// <param name="relationshipType">取得する関係の種類（コアは 0 = RelationProcessorCore）。</param>
    /// <param name="buffer">結果を受け取るバッファー。null の場合は必要なサイズだけを返す。</param>
    /// <param name="returnedLength">バッファーのサイズ（バイト）。必要なサイズ、または書き込んだサイズが返る。</param>
    /// <returns>成功した場合は true。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetLogicalProcessorInformationEx(
        int relationshipType,
        [Out] byte[]? buffer,
        ref uint returnedLength);

    /// <summary>
    /// DXCore のアダプター一覧を作るファクトリーを作る（Windows 10 2004 以降）。
    /// </summary>
    /// <param name="riid">受け取るインターフェースの ID（IDXCoreAdapterFactory）。</param>
    /// <param name="factory">作られたファクトリーの COM ポインター。</param>
    /// <returns>HRESULT。成功した場合は 0 以上。</returns>
    [LibraryImport("dxcore.dll")]
    internal static partial int DXCoreCreateAdapterFactory(in Guid riid, out nint factory);

    /// <summary>
    /// システム全体のメモリ（コミット量、キャッシュ、カーネルのプール）とハンドル・プロセス・スレッドの数を取得する。
    /// </summary>
    /// <param name="information">結果を受け取る構造体。</param>
    /// <param name="size">構造体のサイズ（バイト）。</param>
    /// <returns>成功した場合は true。</returns>
    [LibraryImport("psapi.dll", EntryPoint = "GetPerformanceInfo", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetPerformanceInfo(out PerformanceInformation information, uint size);

    /// <summary>
    /// Win32 の PERFORMANCE_INFORMATION 構造体。メモリの量はすべてページ数で、<see cref="PageSize"/> を掛けるとバイト数になる。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PerformanceInformation
    {
        /// <summary>構造体のサイズ（バイト）。</summary>
        public uint Cb;

        /// <summary>今のコミット量（ページ数）。</summary>
        public nuint CommitTotal;

        /// <summary>コミットの上限（ページ数）。</summary>
        public nuint CommitLimit;

        /// <summary>起動してからのコミット量の最大（ページ数）。</summary>
        public nuint CommitPeak;

        /// <summary>物理メモリの合計（ページ数）。</summary>
        public nuint PhysicalTotal;

        /// <summary>使用可能な物理メモリ（ページ数）。</summary>
        public nuint PhysicalAvailable;

        /// <summary>システムのファイル キャッシュ（ページ数）。</summary>
        public nuint SystemCache;

        /// <summary>カーネルが使っているメモリの合計（ページ数）。</summary>
        public nuint KernelTotal;

        /// <summary>カーネルのページング プール（ページ数）。</summary>
        public nuint KernelPaged;

        /// <summary>カーネルの非ページング プール（ページ数）。</summary>
        public nuint KernelNonpaged;

        /// <summary>1 ページの大きさ（バイト）。</summary>
        public nuint PageSize;

        /// <summary>開かれているハンドルの数。</summary>
        public uint HandleCount;

        /// <summary>プロセスの数。</summary>
        public uint ProcessCount;

        /// <summary>スレッドの数。</summary>
        public uint ThreadCount;
    }

    /// <summary>
    /// Win32 の SYSTEM_POWER_STATUS 構造体。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SystemPowerStatus
    {
        /// <summary>AC 電源の状態。0 = つながっていない、1 = つながっている、255 = 不明。</summary>
        public byte AcLineStatus;

        /// <summary>バッテリーの状態のフラグ。128 = バッテリーがない。</summary>
        public byte BatteryFlag;

        /// <summary>バッテリーの残量（%）。255 = 不明。</summary>
        public byte BatteryLifePercent;

        /// <summary>省電力モード（バッテリー節約機能）の状態。</summary>
        public byte SystemStatusFlag;

        /// <summary>バッテリーの残り時間（秒）。</summary>
        public uint BatteryLifeTime;

        /// <summary>満充電からの持続時間（秒）。</summary>
        public uint BatteryFullLifeTime;
    }

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

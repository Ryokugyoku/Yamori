using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Yamori.Core.Interop;

/// <summary>
/// デバイスへの問い合わせ（DeviceIoControl）の宣言。アクセス権 0 で開いたハンドルで使うため、管理者権限を必要としない。
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>ほかのプロセスの読み取りを妨げない共有モード（FILE_SHARE_READ）。</summary>
    internal const uint FileShareRead = 0x00000001;

    /// <summary>ほかのプロセスの書き込みを妨げない共有モード（FILE_SHARE_WRITE）。</summary>
    internal const uint FileShareWrite = 0x00000002;

    /// <summary>既にあるものだけを開く（OPEN_EXISTING）。</summary>
    internal const uint OpenExisting = 3;

    /// <summary>ストレージのプロパティを問い合わせる制御コード（IOCTL_STORAGE_QUERY_PROPERTY）。FILE_ANY_ACCESS のため、アクセス権 0 で使える。</summary>
    internal const uint IoctlStorageQueryProperty = 0x002D1400;

    /// <summary>デバイスの温度を表すプロパティ（STORAGE_PROPERTY_ID の StorageDeviceTemperatureProperty）。</summary>
    internal const int StorageDeviceTemperatureProperty = 52;

    /// <summary>標準の問い合わせ（STORAGE_QUERY_TYPE の PropertyStandardQuery）。</summary>
    internal const int PropertyStandardQuery = 0;

    /// <summary>STORAGE_PROPERTY_QUERY の大きさ（バイト）。PropertyId・QueryType・AdditionalParameters[1] を 4 バイト境界にそろえた値。</summary>
    internal const int StoragePropertyQuerySize = 12;

    /// <summary>
    /// ファイルやデバイスを開く。
    /// </summary>
    /// <param name="fileName">開く名前（例：<c>\\.\PhysicalDrive0</c>）。</param>
    /// <param name="desiredAccess">求めるアクセス権。0 なら読み書きをせず、属性の問い合わせだけを行う。</param>
    /// <param name="shareMode">共有モード。</param>
    /// <param name="securityAttributes">セキュリティ属性。使わないため 0。</param>
    /// <param name="creationDisposition">開き方。</param>
    /// <param name="flagsAndAttributes">フラグと属性。</param>
    /// <param name="templateFile">テンプレート。使わないため 0。</param>
    /// <returns>ハンドル。失敗した場合は無効なハンドル。</returns>
    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        nint securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        nint templateFile);

    /// <summary>
    /// デバイスに制御コードを送る。
    /// </summary>
    /// <param name="device">デバイスのハンドル。</param>
    /// <param name="ioControlCode">制御コード。</param>
    /// <param name="inBuffer">入力のバッファー。</param>
    /// <param name="inBufferSize">入力のバッファーの大きさ（バイト）。</param>
    /// <param name="outBuffer">出力のバッファー。</param>
    /// <param name="outBufferSize">出力のバッファーの大きさ（バイト）。</param>
    /// <param name="bytesReturned">出力に書き込まれた大きさ（バイト）。</param>
    /// <param name="overlapped">非同期の処理に使う構造体。使わないため 0。</param>
    /// <returns>成功した場合は true。</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeviceIoControl(
        SafeFileHandle device,
        uint ioControlCode,
        byte[] inBuffer,
        int inBufferSize,
        [Out] byte[] outBuffer,
        int outBufferSize,
        out int bytesReturned,
        nint overlapped);
}

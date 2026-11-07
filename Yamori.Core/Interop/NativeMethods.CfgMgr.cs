using System.Runtime.InteropServices;

namespace Yamori.Core.Interop;

/// <summary>
/// 構成マネージャー（CfgMgr32）の API の宣言。デバイスの追加・取り外しの通知を受け取るのに使う。
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>CONFIGRET の成功（CR_SUCCESS）。</summary>
    internal const uint CrSuccess = 0;

    /// <summary>CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE。</summary>
    internal const int CmNotifyFilterTypeDeviceInterface = 0;

    /// <summary>CM_NOTIFY_FILTER_FLAG_ALL_INTERFACE_CLASSES。すべての種類のデバイス インターフェースを対象にする。</summary>
    internal const uint CmNotifyFilterFlagAllInterfaceClasses = 0x1;

    /// <summary>CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL。</summary>
    internal const int CmNotifyActionDeviceInterfaceArrival = 0;

    /// <summary>CM_NOTIFY_ACTION_DEVICEINTERFACEREMOVAL。</summary>
    internal const int CmNotifyActionDeviceInterfaceRemoval = 1;

    /// <summary>MAX_DEVICE_ID_LEN。</summary>
    private const int MaxDeviceIdLength = 200;

    /// <summary>
    /// デバイスの変化の通知を登録する。
    /// </summary>
    /// <param name="filter">通知の対象。</param>
    /// <param name="context">コールバックに渡す値。</param>
    /// <param name="callback">通知を受け取る関数。</param>
    /// <param name="notifyContext">登録のハンドル。<see cref="CM_Unregister_Notification"/> で解除する。</param>
    /// <returns>CONFIGRET。成功は <see cref="CrSuccess"/>。</returns>
    [LibraryImport("CfgMgr32.dll")]
    internal static unsafe partial uint CM_Register_Notification(
        in CmNotifyFilter filter,
        nint context,
        delegate* unmanaged[Stdcall]<nint, nint, int, nint, uint, uint> callback,
        out nint notifyContext);

    /// <summary>
    /// <see cref="CM_Register_Notification"/> の登録を解除する。
    /// </summary>
    /// <param name="notifyContext">登録のハンドル。</param>
    /// <returns>CONFIGRET。成功は <see cref="CrSuccess"/>。</returns>
    [LibraryImport("CfgMgr32.dll")]
    internal static partial uint CM_Unregister_Notification(nint notifyContext);

    /// <summary>CM_NOTIFY_FILTER 構造体（共用体は最も大きい DeviceInstance の形で持つ）。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct CmNotifyFilter
    {
        /// <summary>構造体の大きさ（バイト）。</summary>
        public uint Size;

        /// <summary>CM_NOTIFY_FILTER_FLAG_* の組み合わせ。</summary>
        public uint Flags;

        /// <summary>CM_NOTIFY_FILTER_TYPE_*。</summary>
        public int FilterType;

        /// <summary>予約。0 にする。</summary>
        public uint Reserved;

        /// <summary>共用体の領域（ClassGuid、hTarget、InstanceId のいずれか）。</summary>
        public fixed char InstanceId[MaxDeviceIdLength];
    }
}

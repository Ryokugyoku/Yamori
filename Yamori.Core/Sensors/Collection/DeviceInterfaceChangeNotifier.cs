using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Yamori.Core.Interop;

namespace Yamori.Core.Sensors.Collection;

/// <summary>
/// 構成マネージャーの通知（CM_Register_Notification）で、デバイス インターフェースの追加・取り外しを知らせる。
/// </summary>
/// <remarks>
/// すべての種類のインターフェース（GPU、ディスク、ボリューム、ネットワーク、USB など）が対象のため、関係のない変化でも発生する。
/// 受け取る側でまとめて（デバウンスして）扱うこと。登録に失敗した環境では何も知らせない。
/// </remarks>
public sealed class DeviceInterfaceChangeNotifier : IHardwareChangeNotifier, IDisposable
{
    private GCHandle _self;
    private nint _registration;

    /// <summary>
    /// 通知を登録して作る。
    /// </summary>
    public unsafe DeviceInterfaceChangeNotifier()
    {
        _self = GCHandle.Alloc(this);
        var filter = new NativeMethods.CmNotifyFilter
        {
            Size = (uint)sizeof(NativeMethods.CmNotifyFilter),
            Flags = NativeMethods.CmNotifyFilterFlagAllInterfaceClasses,
            FilterType = NativeMethods.CmNotifyFilterTypeDeviceInterface,
        };

        try
        {
            var result = NativeMethods.CM_Register_Notification(
                filter,
                GCHandle.ToIntPtr(_self),
                &OnNotification,
                out _registration);
            if (result != NativeMethods.CrSuccess)
            {
                _registration = 0;
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // Windows 8 より前の Windows。構成の変化は知らせない（起動時の宣言だけになる）。
            _registration = 0;
        }

        IsActive = _registration != 0;
        if (!IsActive)
        {
            _self.Free();
        }
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <summary>通知を受け取れる状態かどうかを取得する。登録に失敗した環境では false。</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// 通知の登録を解除する。
    /// </summary>
    public void Dispose()
    {
        if (!IsActive)
        {
            return;
        }

        // 解除は、実行中のコールバックが終わるまで待つ。その後でハンドルを解放する。
        _ = NativeMethods.CM_Unregister_Notification(_registration);
        _registration = 0;
        _self.Free();
        IsActive = false;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint OnNotification(nint notify, nint context, int action, nint eventData, uint eventDataSize)
    {
        if (action is NativeMethods.CmNotifyActionDeviceInterfaceArrival or NativeMethods.CmNotifyActionDeviceInterfaceRemoval
            && GCHandle.FromIntPtr(context).Target is DeviceInterfaceChangeNotifier notifier)
        {
            try
            {
                notifier.Changed?.Invoke(notifier, EventArgs.Empty);
            }
            catch (Exception)
            {
                // ネイティブのコールバックの外へ例外を出すとプロセスが落ちるため、ここで止める。
            }
        }

        return NativeMethods.CrSuccess;
    }
}

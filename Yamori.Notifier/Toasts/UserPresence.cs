using Yamori.Notifier.Interop;

namespace Yamori.Notifier.Toasts;

/// <summary>
/// 利用者が通知を受けられる状態かを Windows に問い合わせる。ゲームなどの全画面中やプレゼンテーション中は「忙しい」とみなす。
/// </summary>
internal static unsafe class UserPresence
{
    /// <summary>
    /// 利用者が全画面のアプリやプレゼンテーションを使っていて、通知を控えるべきかどうかを調べる。
    /// </summary>
    /// <returns>控えるべきなら true。問い合わせに失敗したら false（通知を出す側に倒す）。</returns>
    public static bool IsBusy()
    {
        int state;
        if (NativeMethods.SHQueryUserNotificationState(&state) != 0)
        {
            return false;
        }

        return state is NativeMethods.QunsBusy or NativeMethods.QunsRunningD3dFullScreen or NativeMethods.QunsPresentationMode;
    }
}

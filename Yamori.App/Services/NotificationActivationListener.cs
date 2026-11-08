using System;
using System.Runtime.InteropServices;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Yamori.Core.Notifications;

namespace Yamori.Services;

/// <summary>
/// 通知専用の常駐プログラムが出した通知を押したときの知らせを受け取る（Windows App SDK の <see cref="AppNotificationManager"/>）。
/// アプリが閉じていれば Windows が Yamori.exe を起動し、開いていればこのプロセスに知らせが届く。
/// </summary>
public sealed class NotificationActivationListener : IDisposable
{
    private bool _registered;

    /// <summary>
    /// アプリが開いている間に通知が押されたときに起きる。UI スレッドとは別のスレッドで起きる。
    /// </summary>
    public event EventHandler<NotificationActivationRequest>? Activated;

    /// <summary>
    /// 通知の知らせを受け取れるよう登録し、このプロセスが通知を押して起動されたのならその指定を返す。
    /// アプリの起動時に 1 回だけ呼ぶ。
    /// </summary>
    /// <returns>通知を押して起動されたときはその指定。普通の起動なら null。</returns>
    public NotificationActivationRequest? Start()
    {
        var manager = AppNotificationManager.Default;
        try
        {
            // 知らせを取りこぼさないよう、登録より先に受け取り口をつなぐ（Windows App SDK の決まりごと）。
            manager.NotificationInvoked += OnNotificationInvoked;
            manager.Register();
            _registered = true;
        }
        catch (COMException ex)
        {
            // パッケージの外で起動したときなど、通知の起動の宣言がなければ登録できない。通知なしで続ける。
            manager.NotificationInvoked -= OnNotificationInvoked;
            ErrorLog.Write("Notification", ex);
            return null;
        }

        var activated = AppInstance.GetCurrent().GetActivatedEventArgs();
        return activated.Kind == ExtendedActivationKind.AppNotification
            && activated.Data is AppNotificationActivatedEventArgs notification
                ? NotificationActivationArguments.Parse(notification.Argument)
                : null;
    }

    /// <summary>
    /// 登録を解く。アプリを閉じるときに呼ぶ。以後に押された通知は、Windows が新しくアプリを起動して届ける。
    /// </summary>
    public void Dispose()
    {
        if (!_registered)
        {
            return;
        }

        var manager = AppNotificationManager.Default;
        manager.NotificationInvoked -= OnNotificationInvoked;
        try
        {
            manager.Unregister();
        }
        catch (COMException ex)
        {
            ErrorLog.Write("Notification", ex);
        }

        _registered = false;
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args) =>
        Activated?.Invoke(this, NotificationActivationArguments.Parse(args.Argument));
}

using System.Runtime.InteropServices;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Yamori.Core.Notifications;

namespace Yamori.Notifier.Toasts;

/// <summary>
/// Windows の通知（トースト）を出す。通知は、このプロセスが属するアプリ（パッケージの Yamori）の名前で出る。
/// 押したときの処理は Windows がアプリ本体（Yamori.exe）へ届ける（Package.appxmanifest の通知の起動の宣言による）。
/// </summary>
internal sealed class ToastPresenter
{
    /// <summary>
    /// 通知を出す。
    /// </summary>
    /// <param name="title">見出し。</param>
    /// <param name="body">本文。</param>
    /// <param name="activation">押したときにアプリ本体で開くもの。</param>
    /// <returns>出せたら true。パッケージの外で動かしているなど、出せなかったら false。</returns>
    public bool Show(string title, string body, NotificationActivationRequest activation)
    {
        var document = new XmlDocument();
        document.LoadXml(ToastXmlBuilder.Build(title, body, activation));

        try
        {
            ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(document));
            return true;
        }
        catch (COMException ex)
        {
            // パッケージの ID がない（MSIX の外で起動した）と、通知の出し先が決まらず失敗する。
            NotifierLog.Write("Toast", ex);
            return false;
        }
    }
}

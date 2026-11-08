using Yamori.Core.Notifications;
using Yamori.Notifier.Localization;
using Yamori.Notifier.Toasts;

namespace Yamori.Notifier;

/// <summary>
/// 通知専用の常駐プログラム（Yamori.Notifier）の入口。サインイン時に Windows のスタートアップ タスクとして起動し、トレイに常駐する。
/// </summary>
internal static class Program
{
    /// <summary>確かめ用の通知を 1 回だけ出して終わる引数。開発中に、通知と押したときの動きを確かめるために使う。</summary>
    private const string TestNotificationArgument = "--test-notification";

    /// <summary>
    /// 起動する。同じサインインの中ですでに動いていれば、何もせずに終わる。
    /// </summary>
    /// <param name="args">起動の引数。</param>
    /// <returns>終了コード。</returns>
    [STAThread]
    private static int Main(string[] args)
    {
        NotifierLanguage.Apply();

        if (args.Contains(TestNotificationArgument, StringComparer.OrdinalIgnoreCase))
        {
            // 押したときに異常の画面へ移るかを確かめるため、番号 1 の異常として出す。
            var shown = new ToastPresenter().Show(
                NotifierText.Get("Test_Title"),
                NotifierText.Get("Test_Body"),
                NotificationActivationRequest.ForAnomaly(1));
            return shown ? 0 : 1;
        }

        using var mutex = new Mutex(initiallyOwned: true, NotifierInstance.MutexName, out var createdNew);
        if (!createdNew)
        {
            return 0;
        }

        try
        {
            using var application = new NotifierApplication();
            application.Run();
            return 0;
        }
        catch (Exception ex)
        {
            NotifierLog.Write("Main", ex);
            return 1;
        }
    }
}

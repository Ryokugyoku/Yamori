using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Yamori.Core.Notifications;

namespace Yamori.Services;

/// <summary>
/// 通知専用の常駐プログラム（Yamori.Notifier）が動いていなければ起動する。
/// 普段はサインイン時に Windows のスタートアップ タスクが起動するが、入れた直後や「終了」を選んだあとでも、アプリを開けば通知が戻るようにするため。
/// </summary>
public static class NotifierProcessStarter
{
    /// <summary>
    /// 常駐プログラムが動いていなければ起動する。アプリ本体と同じフォルダーにないとき（開発中など）は何もしない。
    /// アプリ本体はパッケージの中で動くので、起動した常駐プログラムも同じアプリの ID を持ち、通知を出せる。
    /// </summary>
    public static void EnsureRunning()
    {
        try
        {
            if (NotifierInstance.IsRunning())
            {
                return;
            }

            var executable = Path.Combine(AppContext.BaseDirectory, NotifierInstance.ExecutableName);
            if (!File.Exists(executable))
            {
                return;
            }

            Process.Start(new ProcessStartInfo(executable) { UseShellExecute = false })?.Dispose();
        }
        catch (Exception ex) when (ex is Win32Exception or IOException or UnauthorizedAccessException)
        {
            // 起動できなくてもアプリ本体は使える。次にアプリを開いたとき、またはサインイン時にもう一度試す。
            ErrorLog.Write("Notifier", ex);
        }
    }
}

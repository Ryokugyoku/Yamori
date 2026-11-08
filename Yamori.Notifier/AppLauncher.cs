using System.ComponentModel;
using System.Diagnostics;
using Windows.ApplicationModel;

namespace Yamori.Notifier;

/// <summary>
/// アプリ本体（Yamori）を開く。パッケージに入っているときは、スタート メニューから開いたときと同じ方法で開く。
/// </summary>
internal static class AppLauncher
{
    /// <summary>パッケージの中のアプリ本体の ID（Package.appxmanifest の Application Id）。</summary>
    private const string ApplicationId = "App";

    /// <summary>
    /// アプリ本体を開く。すでに開いていれば、Windows がそのウィンドウを使う。
    /// </summary>
    public static void OpenApp()
    {
        try
        {
            if (TryGetPackageFamilyName() is { } familyName)
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $@"shell:AppsFolder\{familyName}!{ApplicationId}") { UseShellExecute = true });
                return;
            }

            // パッケージの外（開発中に直接起動したとき）は、同じフォルダーの Yamori.exe を開く。
            var executable = Path.Combine(AppContext.BaseDirectory, "Yamori.exe");
            if (File.Exists(executable))
            {
                Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
            }
        }
        catch (Win32Exception ex)
        {
            NotifierLog.Write("OpenApp", ex);
        }
    }

    private static string? TryGetPackageFamilyName()
    {
        try
        {
            return Package.Current.Id.FamilyName;
        }
        catch (InvalidOperationException)
        {
            // パッケージの ID を持たないプロセス。
            return null;
        }
    }
}

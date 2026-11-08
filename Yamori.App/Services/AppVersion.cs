using System;
using System.Reflection;

namespace Yamori.Services;

/// <summary>
/// Yamori の版を取得する。パッケージとして動いていればパッケージの版、そうでなければ実行ファイルの版を使う。
/// </summary>
public static class AppVersion
{
    /// <summary>
    /// 版を「1.2.3.4」の形で取得する。
    /// </summary>
    /// <returns>版の文字列。</returns>
    public static string Get()
    {
        try
        {
            var version = Windows.ApplicationModel.Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            // パッケージにしないで起動した場合（パッケージの ID がない）。
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
        }
    }
}

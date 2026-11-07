using System.IO.Pipes;
using System.Security.Principal;

namespace Yamori.Core.Sensors.Ipc.Server;

/// <summary>
/// パイプの相手（クライアント）が誰として実行しているかを調べる。
/// </summary>
public static class PipeClientIdentity
{
    /// <summary>
    /// 相手が管理者として（昇格して）実行しているかどうかを調べる。相手から 1 回以上読んだ後に呼ぶこと。
    /// </summary>
    /// <param name="pipe">接続済みのパイプ。</param>
    /// <returns>Administrators のグループが有効なトークンで実行していれば true。調べられない場合は false。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pipe"/> が null。</exception>
    public static bool IsAdministrator(NamedPipeServerStream pipe)
    {
        ArgumentNullException.ThrowIfNull(pipe);

        var result = false;
        try
        {
            pipe.RunAsClient(() =>
            {
                using var identity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query);
                result = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            result = false;
        }

        return result;
    }
}

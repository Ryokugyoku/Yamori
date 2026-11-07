using System.Security.Principal;

namespace Yamori.Helper;

/// <summary>
/// Helper を動かしてよい権限（LocalSystem、または管理者として昇格したコンソール）かどうかを調べる。
/// </summary>
public static class HelperPrivilege
{
    /// <summary>
    /// 今のプロセスが LocalSystem か、Administrators のグループが有効なトークンで動いているかを調べる。
    /// </summary>
    /// <returns>動かしてよければ true。</returns>
    public static bool IsSufficient()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return identity.IsSystem || new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}

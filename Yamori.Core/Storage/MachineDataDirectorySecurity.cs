using System.Security.AccessControl;
using System.Security.Principal;

namespace Yamori.Core.Storage;

/// <summary>
/// Helper のデータフォルダーのアクセス制御。SYSTEM と Administrators だけに許し、親フォルダーからの継承は切る。
/// </summary>
public static class MachineDataDirectorySecurity
{
    /// <summary>
    /// フォルダーを作り、アクセス制御を設定する。
    /// </summary>
    /// <param name="path">フォルダーのパス。</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> が null。</exception>
    /// <exception cref="UnauthorizedAccessException">設定する権限がない（管理者でも SYSTEM でもない）。</exception>
    /// <exception cref="IOException">フォルダーを作れない。</exception>
    public static void EnsureDirectory(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var directory = new DirectoryInfo(path);
        if (!directory.Exists)
        {
            directory.Create();
        }

        directory.SetAccessControl(Create());
    }

    /// <summary>
    /// 設定するアクセス制御を作る。
    /// </summary>
    /// <returns>アクセス制御。</returns>
    public static DirectorySecurity Create()
    {
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        const InheritanceFlags inherit = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
        security.AddAccessRule(new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));
        return security;
    }
}

using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Yamori.Core.Sensors.Ipc.Server;

/// <summary>
/// センサーのパイプのアクセス制御。SYSTEM と Administrators にすべての操作を、対話的にサインインしているユーザーに読み書き（つなぐこと）を許し、
/// ネットワークからの接続は拒否する。どの操作を受け付けるかは <see cref="SensorRequestHandler"/> で決める。
/// </summary>
public static class SensorPipeSecurity
{
    /// <summary>
    /// 標準のアクセス制御を作る。
    /// </summary>
    /// <returns>パイプに設定するアクセス制御。</returns>
    public static PipeSecurity CreateDefault()
    {
        var security = new PipeSecurity();
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.InteractiveSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid, null), PipeAccessRights.FullControl, AccessControlType.Deny));
        return security;
    }
}

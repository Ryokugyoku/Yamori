namespace Yamori.Core.Sensors.Extended;

/// <summary>
/// ドライバーのサービスの登録（<c>HKLM\SYSTEM\CurrentControlSet\Services\PawnIO</c>）があるかどうかで、PawnIO が入っているかを調べる。
/// </summary>
/// <remarks>
/// 読み取りだけを行う。公式版（署名済み）かどうかの確認は、PawnIO を実際に使う Helper の側で行う。
/// </remarks>
public sealed class RegistryPawnIoInstallationProbe : IPawnIoInstallationProbe
{
    private const string ServiceKeyPath = @"SYSTEM\CurrentControlSet\Services\PawnIO";

    /// <inheritdoc />
    public bool IsInstalled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(ServiceKeyPath, writable: false);
            return key is not null;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }
}

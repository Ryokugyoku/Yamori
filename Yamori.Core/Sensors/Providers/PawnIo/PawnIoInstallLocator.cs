namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// PawnIO を入れたフォルダーを探す。読み取りだけを行う。
/// </summary>
public static class PawnIoInstallLocator
{
    private const string UninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO";

    /// <summary>
    /// PawnIO を入れたフォルダーを探す。インストーラーの登録（<c>InstallLocation</c>）を見て、なければ <c>%ProgramFiles%\PawnIO</c> を使う。
    /// </summary>
    /// <returns>フォルダーのパス。見つからなければ null。</returns>
    public static string? TryFindInstallDirectory()
    {
        string? location = null;
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(UninstallKeyPath, writable: false);
            location = key?.GetValue("InstallLocation") as string;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            location = null;
        }

        var candidates = new[]
        {
            location,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PawnIO"),
        };
        return candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c) && Directory.Exists(c));
    }
}

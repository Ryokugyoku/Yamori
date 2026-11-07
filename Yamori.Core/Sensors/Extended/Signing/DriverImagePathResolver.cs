namespace Yamori.Core.Sensors.Extended.Signing;

/// <summary>
/// ドライバーのサービスの登録（<c>ImagePath</c>）から、ドライバーのファイルの絶対パスを求める。読み取りだけを行う。
/// </summary>
public static class DriverImagePathResolver
{
    /// <summary>
    /// サービス名からドライバーのファイルのパスを求める。
    /// </summary>
    /// <param name="serviceName">ドライバーのサービス名（例：<c>PawnIO</c>）。</param>
    /// <returns>ファイルの絶対パス。登録がない、または読めない場合は null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceName"/> が null。</exception>
    public static string? TryResolve(string serviceName)
    {
        ArgumentNullException.ThrowIfNull(serviceName);
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}", writable: false);
            return key?.GetValue("ImagePath", null, Microsoft.Win32.RegistryValueOptions.None) is string imagePath ? Normalize(imagePath) : null;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// <c>ImagePath</c> の書き方（<c>\SystemRoot\…</c>、<c>System32\…</c>、<c>\??\C:\…</c>、環境変数）を、絶対パスにそろえる。
    /// </summary>
    /// <param name="imagePath">レジストリの <c>ImagePath</c> の値。</param>
    /// <returns>絶対パス。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="imagePath"/> が null。</exception>
    public static string Normalize(string imagePath)
    {
        ArgumentNullException.ThrowIfNull(imagePath);

        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var path = Environment.ExpandEnvironmentVariables(imagePath.Trim().Trim('"'));
        if (path.StartsWith(@"\??\", StringComparison.Ordinal))
        {
            return path[4..];
        }

        if (path.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(windows, path[@"\SystemRoot\".Length..]);
        }

        return Path.IsPathRooted(path) ? path : Path.Combine(windows, path);
    }
}

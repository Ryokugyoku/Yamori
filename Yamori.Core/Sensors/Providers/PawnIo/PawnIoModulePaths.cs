namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// Helper に同梱した PawnIO.Modules のモジュールの置き場所。モジュールは公式リリースのものを変更せずに入れる。
/// </summary>
public static class PawnIoModulePaths
{
    /// <summary>モジュールを入れるフォルダーの名前（Helper の実行ファイルの隣）。</summary>
    public const string FolderName = "PawnIoModules";

    /// <summary>
    /// IntelMSR モジュールのパスを取得する。
    /// </summary>
    /// <param name="baseDirectory">Helper の実行ファイルのフォルダー（<see cref="AppContext.BaseDirectory"/>）。</param>
    /// <returns><c>{baseDirectory}\PawnIoModules\IntelMSR.bin</c>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="baseDirectory"/> が null。</exception>
    public static string IntelMsr(string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(baseDirectory);
        return Path.Combine(baseDirectory, FolderName, "IntelMSR.bin");
    }
}

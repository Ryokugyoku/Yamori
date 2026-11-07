namespace Yamori.Core.Storage;

/// <summary>
/// Helper（サービス）だけが書き込む、PC 全体で 1 つのデータの置き場所。GUI はこのフォルダーを開かず、IPC で扱う。
/// </summary>
/// <remarks>
/// MSIX のパッケージのマシン共通フォルダー（<c>%ProgramData%\Microsoft\Windows\AppRepository\Families\ApplicationData\{パッケージ ファミリ名}\Machine</c>）が
/// あればそこを使う（パッケージを消すと一緒に消える）。なければ <c>%ProgramData%\Yamori</c> を使う。
/// </remarks>
public static class MachineDataPaths
{
    /// <summary>センサーのデータを置くフォルダーの名前。</summary>
    public const string SensorsFolderName = "sensors";

    /// <summary>
    /// Helper のデータフォルダーを取得する。
    /// </summary>
    /// <returns>フォルダーの絶対パス。</returns>
    public static string GetDataDirectory() =>
        TryGetPackageMachineFolder()
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), YamoriDataPaths.AppFolderName);

    /// <summary>
    /// センサーの時系列（<c>timeseries</c>）・マスタ（<c>master.db</c>）・優先度の設定（<c>sensor-priority.json</c>）を置くフォルダーを取得する。
    /// </summary>
    /// <returns>フォルダーの絶対パス。</returns>
    public static string GetSensorDataDirectory() => Path.Combine(GetDataDirectory(), SensorsFolderName);

    /// <summary>
    /// センサーの取得元の優先度を決める設定ファイルのパスを取得する。ファイルがなければ既定の優先度を使う。
    /// </summary>
    /// <returns>設定ファイルの絶対パス。</returns>
    public static string GetSensorPriorityFilePath() => Path.Combine(GetSensorDataDirectory(), "sensor-priority.json");

    /// <summary>
    /// PawnIO の利用の同意を保存するファイルのパスを取得する。
    /// </summary>
    /// <returns>ファイルの絶対パス。</returns>
    public static string GetPawnIoConsentFilePath() => Path.Combine(GetSensorDataDirectory(), "pawnio-consent.json");

    /// <summary>
    /// パッケージのマシン共通フォルダーを探す。
    /// </summary>
    /// <returns>パッケージとして動いていて、フォルダーがあればそのパス。なければ null。</returns>
    public static string? TryGetPackageMachineFolder()
    {
        var familyName = TryGetCurrentPackageFamilyName();
        if (familyName is null)
        {
            return null;
        }

        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Microsoft", "Windows", "AppRepository", "Families", "ApplicationData", familyName, "Machine");
        return Directory.Exists(path) ? path : null;
    }

    private static string? TryGetCurrentPackageFamilyName()
    {
        uint length = 0;
        var error = PackageIdentityNative.GetCurrentPackageFamilyName(ref length, null);
        if (error != PackageIdentityNative.ErrorInsufficientBuffer || length == 0)
        {
            return null;
        }

        var buffer = new char[length];
        error = PackageIdentityNative.GetCurrentPackageFamilyName(ref length, buffer);
        return error == 0 ? new string(buffer, 0, (int)length - 1) : null;
    }
}

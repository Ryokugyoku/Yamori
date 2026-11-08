namespace Yamori.Core.Storage;

/// <summary>
/// 画面のプロセスがユーザーごとに置くファイルの場所。すべてユーザーのローカルフォルダーの中にあり、PC の外には出さない。
/// </summary>
/// <remarks>センサーの記録（時系列・マスタ）は Helper だけが <see cref="MachineDataPaths"/> に置く。</remarks>
public static class YamoriDataPaths
{
    /// <summary>
    /// Yamori 用のフォルダーの名前。
    /// </summary>
    public const string AppFolderName = "Yamori";

    /// <summary>
    /// Yamori のユーザーごとのデータフォルダー（<c>%LOCALAPPDATA%\Yamori</c>）を取得する。
    /// </summary>
    /// <returns>データフォルダーの絶対パス。</returns>
    public static string GetDataDirectory() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);

    /// <summary>
    /// 設定ファイルのパスを取得する。
    /// </summary>
    /// <returns>設定ファイルの絶対パス。</returns>
    public static string GetSettingsFilePath() => Path.Combine(GetDataDirectory(), "settings.json");

    /// <summary>
    /// Helper が動いていない間の代わりの表示で使う、センサーの取得元の優先度の設定ファイルのパスを取得する。ファイルがなければ既定の優先度を使う。
    /// </summary>
    /// <returns>設定ファイルの絶対パス。</returns>
    public static string GetSensorPriorityFilePath() => Path.Combine(GetDataDirectory(), "sensor-priority.json");

    /// <summary>
    /// 画面で起きた想定外のエラーを書き残すファイルのパスを取得する。落ちる原因を調べるためのもので、PC の外には送らない。
    /// </summary>
    /// <returns>エラーの記録ファイルの絶対パス。</returns>
    public static string GetErrorLogFilePath() => Path.Combine(GetDataDirectory(), "errors.log");
}

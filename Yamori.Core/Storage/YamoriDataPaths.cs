namespace Yamori.Core.Storage;

/// <summary>
/// Yamori がデータを保存する場所。すべてユーザーのローカルフォルダーの中にあり、PC の外には出さない。
/// </summary>
public static class YamoriDataPaths
{
    /// <summary>
    /// Yamori のデータフォルダー（<c>%LOCALAPPDATA%\Yamori</c>）を取得する。
    /// </summary>
    /// <returns>データフォルダーの絶対パス。</returns>
    public static string GetDataDirectory() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Yamori");

    /// <summary>
    /// 使用率の履歴を置くフォルダーを取得する。
    /// </summary>
    /// <returns>履歴フォルダーの絶対パス。</returns>
    public static string GetHistoryDirectory() => Path.Combine(GetDataDirectory(), HistoryFolderName);

    /// <summary>
    /// 設定ファイルのパスを取得する。記録の保存先を変えても、設定ファイルは常にこの PC の中に置く。
    /// </summary>
    /// <returns>設定ファイルの絶対パス。</returns>
    public static string GetSettingsFilePath() => Path.Combine(GetDataDirectory(), "settings.json");

    /// <summary>
    /// Helper が動いていない間の代わりの表示で使う、センサーの取得元の優先度の設定ファイルのパスを取得する。ファイルがなければ既定の優先度を使う。
    /// </summary>
    /// <remarks>センサーの記録（時系列・マスタ）は Helper だけが <see cref="MachineDataPaths"/> に置く。</remarks>
    /// <returns>設定ファイルの絶対パス。</returns>
    public static string GetSensorPriorityFilePath() => Path.Combine(GetDataDirectory(), "sensor-priority.json");

    /// <summary>
    /// 履歴を置くフォルダーの名前。
    /// </summary>
    public const string HistoryFolderName = "history";

    /// <summary>
    /// PC の外（OneDrive や指定フォルダー）に作る、Yamori 用フォルダーの名前。
    /// </summary>
    public const string AppFolderName = "Yamori";
}

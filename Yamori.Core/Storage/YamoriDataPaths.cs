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
    public static string GetHistoryDirectory() => Path.Combine(GetDataDirectory(), "history");
}

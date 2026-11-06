namespace Yamori.Core.Storage;

/// <summary>
/// この PC の OneDrive フォルダーを探す。
/// </summary>
public interface IOneDriveLocator
{
    /// <summary>
    /// OneDrive フォルダーを探す。
    /// </summary>
    /// <returns>OneDrive フォルダーの絶対パス。OneDrive が設定されていなければ null。</returns>
    string? FindRoot();
}

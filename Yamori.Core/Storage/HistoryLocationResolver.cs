using Yamori.Core.Settings;

namespace Yamori.Core.Storage;

/// <summary>
/// 設定から、記録の実際の保存先フォルダーを決める。
/// </summary>
/// <remarks>
/// OneDrive と指定フォルダーでは <c>&lt;保存先&gt;\Yamori\&lt;PC 名&gt;\history</c> に保存する。
/// 複数の PC が同じ OneDrive を使っても、記録が混ざらないようにするため。
/// </remarks>
public sealed class HistoryLocationResolver
{
    private readonly IOneDriveLocator _oneDriveLocator;
    private readonly string _localHistoryDirectory;
    private readonly string _machineName;
    private readonly Func<string, bool> _directoryExists;

    /// <summary>
    /// 必要な情報を指定して作る。
    /// </summary>
    /// <param name="oneDriveLocator">OneDrive フォルダーを探す処理。</param>
    /// <param name="localHistoryDirectory">この PC の中の履歴フォルダー。</param>
    /// <param name="machineName">PC 名。保存先のフォルダー名に使う。</param>
    /// <param name="directoryExists">フォルダーがあるかを調べる処理。省略時は <see cref="Directory.Exists(string)"/>。</param>
    /// <exception cref="ArgumentNullException"><paramref name="oneDriveLocator"/> が null。</exception>
    /// <exception cref="ArgumentException"><paramref name="localHistoryDirectory"/> か <paramref name="machineName"/> が空。</exception>
    public HistoryLocationResolver(
        IOneDriveLocator oneDriveLocator,
        string localHistoryDirectory,
        string machineName,
        Func<string, bool>? directoryExists = null)
    {
        ArgumentNullException.ThrowIfNull(oneDriveLocator);
        ArgumentException.ThrowIfNullOrWhiteSpace(localHistoryDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(machineName);

        _oneDriveLocator = oneDriveLocator;
        _localHistoryDirectory = localHistoryDirectory;
        _machineName = ToSafeFolderName(machineName);
        _directoryExists = directoryExists ?? Directory.Exists;
    }

    /// <summary>
    /// 設定から保存先を決める。選んだ保存先が使えなければ、この PC の中を返す。
    /// </summary>
    /// <param name="settings">設定。</param>
    /// <returns>保存先。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> が null。</exception>
    public HistoryLocation Resolve(YamoriSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var root = settings.StorageLocation switch
        {
            StorageLocationKind.OneDrive => _oneDriveLocator.FindRoot(),
            StorageLocationKind.CustomFolder => ValidCustomFolder(settings.CustomFolderPath),
            _ => null,
        };

        if (settings.StorageLocation == StorageLocationKind.Local)
        {
            return GetLocal(isFallback: false);
        }

        // 選んだ保存先が見つからない（OneDrive からサインアウトした、USB ドライブを外したなど）ときも記録は止めない。
        return root is null
            ? GetLocal(isFallback: true)
            : new HistoryLocation(
                Path.Combine(root, YamoriDataPaths.AppFolderName, _machineName, YamoriDataPaths.HistoryFolderName),
                settings.StorageLocation,
                IsFallback: false);
    }

    /// <summary>
    /// この PC の中の保存先を取得する。
    /// </summary>
    /// <param name="isFallback">選んだ保存先の代わりとして使うなら true。</param>
    /// <returns>この PC の中の保存先。</returns>
    public HistoryLocation GetLocal(bool isFallback) =>
        new(_localHistoryDirectory, StorageLocationKind.Local, isFallback);

    private string? ValidCustomFolder(string? path) =>
        !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path) && _directoryExists(path) ? path : null;

    private static string ToSafeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}

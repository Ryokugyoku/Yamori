using System.Text.Json;

namespace Yamori.Core.Storage.Legacy;

/// <summary>
/// 以前の版が使用率の記録（CSV）を置いていたかもしれないフォルダーを挙げる。
/// </summary>
/// <remarks>
/// 以前の版は、この PC の中（<c>%LOCALAPPDATA%\Yamori\history</c>）か、
/// OneDrive・指定フォルダーの <c>Yamori\&lt;PC 名&gt;\history</c> に保存し、保存先を切り替えるとコピーしていた。
/// どこに残っているか分からないため、考えられる場所をすべて挙げる。指定フォルダーは、設定ファイルに残っている <c>CustomFolderPath</c> から読む。
/// </remarks>
public sealed class LegacyHistoryLocator
{
    /// <summary>以前の版が CSV を置いていたフォルダーの名前。</summary>
    public const string HistoryFolderName = "history";

    private static readonly string[] OneDriveVariableNames = ["OneDrive", "OneDriveConsumer", "OneDriveCommercial"];

    private readonly string _localDataDirectory;
    private readonly string _settingsFilePath;
    private readonly string _machineName;
    private readonly Func<string, string?> _getEnvironmentVariable;

    /// <summary>
    /// 探す材料を指定して作る。
    /// </summary>
    /// <param name="localDataDirectory">ユーザーごとのデータフォルダー（<see cref="YamoriDataPaths.GetDataDirectory"/>）。</param>
    /// <param name="settingsFilePath">設定ファイルのパス（以前の版の指定フォルダーを読むため）。</param>
    /// <param name="machineName">PC 名（以前の版がフォルダー名に使った）。</param>
    /// <param name="getEnvironmentVariable">環境変数を読む処理（OneDrive のフォルダーを探すため）。</param>
    /// <exception cref="ArgumentException"><paramref name="localDataDirectory"/>・<paramref name="settingsFilePath"/>・<paramref name="machineName"/> が空。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="getEnvironmentVariable"/> が null。</exception>
    public LegacyHistoryLocator(
        string localDataDirectory,
        string settingsFilePath,
        string machineName,
        Func<string, string?> getEnvironmentVariable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localDataDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(machineName);
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        _localDataDirectory = localDataDirectory;
        _settingsFilePath = settingsFilePath;
        _machineName = ToSafeFolderName(machineName);
        _getEnvironmentVariable = getEnvironmentVariable;
    }

    /// <summary>
    /// 実際の PC の場所で作る。
    /// </summary>
    /// <returns>この PC・このユーザーの場所を探す <see cref="LegacyHistoryLocator"/>。</returns>
    public static LegacyHistoryLocator CreateDefault() =>
        new(YamoriDataPaths.GetDataDirectory(), YamoriDataPaths.GetSettingsFilePath(), Environment.MachineName, Environment.GetEnvironmentVariable);

    /// <summary>
    /// 考えられる場所を挙げる。フォルダーがあるかどうかは確かめない。
    /// </summary>
    /// <returns>重複のない場所の一覧。</returns>
    public IReadOnlyList<LegacyHistoryLocation> Find()
    {
        var locations = new List<LegacyHistoryLocation>
        {
            // この PC の中。親（%LOCALAPPDATA%\Yamori）には設定ファイルがあるため消さない。
            new(Path.Combine(_localDataDirectory, HistoryFolderName), []),
        };

        var roots = OneDriveVariableNames
            .Select(_getEnvironmentVariable)
            .Append(ReadCustomFolderPath());
        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root) || !Path.IsPathFullyQualified(root))
            {
                continue;
            }

            var appFolder = Path.Combine(root, YamoriDataPaths.AppFolderName);
            var machineFolder = Path.Combine(appFolder, _machineName);
            locations.Add(new(Path.Combine(machineFolder, HistoryFolderName), [machineFolder, appFolder]));
        }

        return locations
            .DistinctBy(l => Path.GetFullPath(l.Directory), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// 設定ファイルに残っている、以前の版の指定フォルダーを読む。
    /// </summary>
    /// <returns>指定フォルダー。なければ null。</returns>
    private string? ReadCustomFolderPath()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return null;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(_settingsFilePath));
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("CustomFolderPath", out var value)
                && value.ValueKind == JsonValueKind.String
                    ? value.GetString()
                    : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// 以前の版と同じ規則で、PC 名をフォルダー名に使える形にする。
    /// </summary>
    private static string ToSafeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}

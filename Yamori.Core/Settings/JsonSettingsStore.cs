using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yamori.Core.Settings;

/// <summary>
/// 設定を JSON ファイルとして保存する。設定ファイルはユーザーごとに、この PC の中に置く。知らない項目は読み込むときに無視する。
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _filePath;

    /// <summary>
    /// 設定ファイルのパスを指定して作る。
    /// </summary>
    /// <param name="filePath">設定ファイルの絶対パス。</param>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> が空。</exception>
    public JsonSettingsStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    /// <inheritdoc />
    public YamoriSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return YamoriSettings.Default;
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<YamoriSettings>(json, SerializerOptions) ?? YamoriSettings.Default;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // 壊れた設定ファイルで起動できなくなるのを避け、既定値で動かす。
            return YamoriSettings.Default;
        }
    }

    /// <inheritdoc />
    public void Save(YamoriSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var temporaryPath = _filePath + ".tmp";

        // 書き込み途中で終了してもファイルが壊れないよう、一時ファイルに書いてから置き換える。
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, SerializerOptions));
        File.Move(temporaryPath, _filePath, overwrite: true);
    }
}

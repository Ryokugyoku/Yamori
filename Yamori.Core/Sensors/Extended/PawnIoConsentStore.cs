using System.Text.Json;

namespace Yamori.Core.Sensors.Extended;

/// <summary>
/// PawnIO の利用の同意を、Helper のデータフォルダーの JSON ファイルに保存する。
/// </summary>
/// <remarks>ファイルがない、または読めない場合は「同意していない」として扱う。</remarks>
public sealed class PawnIoConsentStore
{
    private readonly string _filePath;

    /// <summary>
    /// 保存先のファイルを指定して作る。
    /// </summary>
    /// <param name="filePath">保存先のファイルのパス。</param>
    /// <exception cref="ArgumentNullException"><paramref name="filePath"/> が null。</exception>
    public PawnIoConsentStore(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        _filePath = filePath;
    }

    /// <summary>
    /// 保存されている同意を読む。
    /// </summary>
    /// <returns>同意していれば true。</returns>
    public bool Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return false;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(_filePath));
            return document.RootElement.TryGetProperty("enabled", out var enabled) && enabled.ValueKind == JsonValueKind.True;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// 同意を保存する。一時ファイルに書いてから置き換える。
    /// </summary>
    /// <param name="enabled">同意するなら true。</param>
    /// <exception cref="IOException">書き込みに失敗した。</exception>
    /// <exception cref="UnauthorizedAccessException">書き込む権限がない。</exception>
    public void Save(bool enabled)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporary = _filePath + ".tmp";
        File.WriteAllText(temporary, enabled ? """{ "enabled": true }""" : """{ "enabled": false }""");
        File.Move(temporary, _filePath, overwrite: true);
    }
}

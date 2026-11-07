using System.Text.Json;

namespace Yamori.Core.Sensors.Registry;

/// <summary>
/// 優先度の設定ファイル（JSON）を読む。ファイルがない、または読めない場合は既定の設定を使う。
/// </summary>
/// <remarks>
/// ファイルの形式：
/// <code>
/// {
///   "defaultOrder": ["pawnio", "win-std"],
///   "rules": [ { "pattern": "gpu/*/*/*", "order": ["win-std"] } ]
/// }
/// </code>
/// </remarks>
public sealed class SensorPriorityOptionsLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _path;

    /// <summary>
    /// 設定ファイルのパスを指定して作る。
    /// </summary>
    /// <param name="path">設定ファイルのパス。</param>
    /// <exception cref="ArgumentException"><paramref name="path"/> が null か空。</exception>
    public SensorPriorityOptionsLoader(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        _path = path;
    }

    /// <summary>
    /// 設定を読む。
    /// </summary>
    /// <returns>読んだ設定。ファイルがない、形式が正しくない、パターンの表記が正しくない場合は <see cref="SensorPriorityOptions.Default"/>。</returns>
    public SensorPriorityOptions Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return SensorPriorityOptions.Default;
            }

            var options = JsonSerializer.Deserialize<SensorPriorityOptions>(File.ReadAllText(_path), JsonOptions);
            if (options?.DefaultOrder is null)
            {
                return SensorPriorityOptions.Default;
            }

            options = options with { Rules = options.Rules ?? [] };

            // パターンの表記をここで確かめ、正しくない設定は丸ごと使わない（一部だけ効く状態を避ける）。
            _ = new SensorPriorityPolicy(options);
            return options;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or FormatException)
        {
            return SensorPriorityOptions.Default;
        }
    }
}

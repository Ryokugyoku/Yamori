using System.Text.Json;

namespace Yamori.Core.Anomalies.Storage;

/// <summary>
/// 判断の理由の数値（名前と値の組）と、マスタに置く JSON の間の変換。
/// </summary>
public static class AnomalyEvidenceJson
{
    /// <summary>
    /// JSON にする。有限でない値（NaN や無限大）は書かない。
    /// </summary>
    /// <param name="evidence">判断の理由の数値。</param>
    /// <returns>JSON のオブジェクトの文字列。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="evidence"/> が null。</exception>
    public static string Serialize(IReadOnlyDictionary<string, double> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var finite = evidence.Where(e => double.IsFinite(e.Value)).ToDictionary(e => e.Key, e => e.Value, StringComparer.Ordinal);
        return JsonSerializer.Serialize(finite, AnomalyStorageJsonContext.Default.DictionaryStringDouble);
    }

    /// <summary>
    /// JSON から戻す。読めない場合は空にする（壊れた行で一覧の読み出しを止めないため）。
    /// </summary>
    /// <param name="json">JSON のオブジェクトの文字列。</param>
    /// <returns>判断の理由の数値。</returns>
    public static IReadOnlyDictionary<string, double> Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new Dictionary<string, double>(StringComparer.Ordinal);
        }

        try
        {
            var values = JsonSerializer.Deserialize(json, AnomalyStorageJsonContext.Default.DictionaryStringDouble);
            return values is null
                ? new Dictionary<string, double>(StringComparer.Ordinal)
                : new Dictionary<string, double>(values, StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, double>(StringComparer.Ordinal);
        }
    }
}

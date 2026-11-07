namespace Yamori.Core.Sensors;

/// <summary>
/// 区切りごとに <c>*</c>（どれでもよい）を使える、センサーキーのパターン。サマリーカードの候補や、優先度の規則で使う。
/// </summary>
/// <remarks>例：<c>gpu/*/load/max-engine</c> は、どの GPU の <c>load/max-engine</c> にも一致する。</remarks>
public sealed class SensorKeyPattern
{
    private readonly HardwareKind? _hardwareKind;
    private readonly string? _hardwareId;
    private readonly SensorType? _sensorType;
    private readonly string? _name;
    private readonly string _text;

    private SensorKeyPattern(HardwareKind? hardwareKind, string? hardwareId, SensorType? sensorType, string? name, string text)
    {
        _hardwareKind = hardwareKind;
        _hardwareId = hardwareId;
        _sensorType = sensorType;
        _name = name;
        _text = text;
    }

    /// <summary>
    /// 文字列表記からパターンを読み取る。
    /// </summary>
    /// <param name="text">4 つの区切りを <c>/</c> でつないだ表記。各区切りは正規化済みの値か <c>*</c>。</param>
    /// <returns>読み取ったパターン。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> が null。</exception>
    /// <exception cref="FormatException">表記が正しくない。</exception>
    public static SensorKeyPattern Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var parts = text.Split(SensorKeyTokens.Separator);
        if (parts.Length != 4)
        {
            throw new FormatException($"センサーキーのパターンは 4 つの区切りが必要です: '{text}'");
        }

        HardwareKind? kind = null;
        if (!IsWildcard(parts[0]))
        {
            kind = SensorKeyTokens.TryParseHardwareKind(parts[0], out var k)
                ? k
                : throw new FormatException($"ハードウェアの種類が正しくありません: '{parts[0]}'");
        }

        SensorType? type = null;
        if (!IsWildcard(parts[2]))
        {
            type = SensorKeyTokens.TryParseSensorType(parts[2], out var t)
                ? t
                : throw new FormatException($"量の種類が正しくありません: '{parts[2]}'");
        }

        return new SensorKeyPattern(kind, ParseSegment(parts[1]), type, ParseSegment(parts[3]), text);
    }

    /// <summary>
    /// キーがこのパターンに一致するかどうかを調べる。
    /// </summary>
    /// <param name="key">調べるキー。</param>
    /// <returns>一致すれば true。</returns>
    public bool Matches(SensorKey key) =>
        (_hardwareKind is null || _hardwareKind == key.HardwareKind)
        && (_hardwareId is null || string.Equals(_hardwareId, key.HardwareId, StringComparison.Ordinal))
        && (_sensorType is null || _sensorType == key.SensorType)
        && (_name is null || string.Equals(_name, key.Name, StringComparison.Ordinal));

    /// <summary>
    /// 文字列表記を取得する。
    /// </summary>
    /// <returns>読み取ったときの表記。</returns>
    public override string ToString() => _text;

    private static bool IsWildcard(string segment) => segment == SensorKeyTokens.Wildcard;

    private static string? ParseSegment(string segment) =>
        IsWildcard(segment) ? null
        : SensorKeyTokens.IsNormalizedSegment(segment) ? segment
        : throw new FormatException($"区切りが正規化されていません: '{segment}'");
}

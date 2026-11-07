using System.Diagnostics.CodeAnalysis;

namespace Yamori.Core.Sensors;

/// <summary>
/// <see cref="SensorKey"/> の文字列表記で使う語（ハードウェアの種類と量の種類の表記）と、区切りの正規化の規則。
/// </summary>
public static class SensorKeyTokens
{
    /// <summary>キーの区切り文字。</summary>
    public const char Separator = '/';

    /// <summary>パターンで「どれでもよい」を表す区切り。</summary>
    public const string Wildcard = "*";

    private static readonly Dictionary<HardwareKind, string> HardwareKindTokens = new()
    {
        [HardwareKind.Cpu] = "cpu",
        [HardwareKind.Memory] = "memory",
        [HardwareKind.Gpu] = "gpu",
        [HardwareKind.Storage] = "storage",
        [HardwareKind.Volume] = "volume",
        [HardwareKind.Network] = "network",
        [HardwareKind.Battery] = "battery",
        [HardwareKind.Motherboard] = "motherboard",
        [HardwareKind.ThermalZone] = "thermal-zone",
        [HardwareKind.System] = "system",
        [HardwareKind.PowerSupply] = "psu",
    };

    private static readonly Dictionary<SensorType, string> SensorTypeTokens = new()
    {
        [SensorType.Load] = "load",
        [SensorType.Clock] = "clock",
        [SensorType.Temperature] = "temperature",
        [SensorType.Power] = "power",
        [SensorType.Voltage] = "voltage",
        [SensorType.Current] = "current",
        [SensorType.Fan] = "fan",
        [SensorType.Control] = "control",
        [SensorType.Data] = "data",
        [SensorType.Throughput] = "throughput",
        [SensorType.Rate] = "rate",
        [SensorType.Count] = "count",
        [SensorType.Duration] = "duration",
        [SensorType.Level] = "level",
        [SensorType.Energy] = "energy",
    };

    private static readonly Dictionary<string, HardwareKind> HardwareKindsByToken =
        HardwareKindTokens.ToDictionary(x => x.Value, x => x.Key, StringComparer.Ordinal);

    private static readonly Dictionary<string, SensorType> SensorTypesByToken =
        SensorTypeTokens.ToDictionary(x => x.Value, x => x.Key, StringComparer.Ordinal);

    /// <summary>
    /// ハードウェアの種類の表記を取得する。
    /// </summary>
    /// <param name="kind">ハードウェアの種類。</param>
    /// <returns>キーで使う表記（例：<c>cpu</c>）。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> が定義されていない値。</exception>
    public static string ToToken(HardwareKind kind) =>
        HardwareKindTokens.TryGetValue(kind, out var token)
            ? token
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "定義されていないハードウェアの種類です。");

    /// <summary>
    /// 量の種類の表記を取得する。
    /// </summary>
    /// <param name="type">量の種類。</param>
    /// <returns>キーで使う表記（例：<c>temperature</c>）。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> が定義されていない値。</exception>
    public static string ToToken(SensorType type) =>
        SensorTypeTokens.TryGetValue(type, out var token)
            ? token
            : throw new ArgumentOutOfRangeException(nameof(type), type, "定義されていない量の種類です。");

    /// <summary>
    /// 表記からハードウェアの種類を読み取る。
    /// </summary>
    /// <param name="token">表記。大文字と小文字を区別する。</param>
    /// <param name="kind">読み取った種類。</param>
    /// <returns>読み取れたら true。</returns>
    public static bool TryParseHardwareKind(string token, out HardwareKind kind) =>
        HardwareKindsByToken.TryGetValue(token, out kind);

    /// <summary>
    /// 表記から量の種類を読み取る。
    /// </summary>
    /// <param name="token">表記。大文字と小文字を区別する。</param>
    /// <param name="type">読み取った種類。</param>
    /// <returns>読み取れたら true。</returns>
    public static bool TryParseSensorType(string token, out SensorType type) =>
        SensorTypesByToken.TryGetValue(token, out type);

    /// <summary>
    /// ハードウェア ID やセンサー名を、キーで使える形に正規化する。小文字にし、英数字と <c>.</c> <c>_</c> <c>-</c> 以外は <c>-</c> に置き換え、
    /// 続いた <c>-</c> は 1 つにまとめ、前後の <c>-</c> は取り除く。
    /// </summary>
    /// <param name="value">元の文字列。</param>
    /// <returns>正規化した文字列。使える文字が残らなければ空文字列。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> が null。</exception>
    public static string NormalizeSegment(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var buffer = new char[value.Length];
        var length = 0;
        foreach (var c in value)
        {
            var lower = char.ToLowerInvariant(c);
            var mapped = IsAllowed(lower) ? lower : '-';
            if (mapped == '-' && (length == 0 || buffer[length - 1] == '-'))
            {
                continue;
            }

            buffer[length++] = mapped;
        }

        while (length > 0 && buffer[length - 1] == '-')
        {
            length--;
        }

        return new string(buffer, 0, length);
    }

    /// <summary>
    /// 文字列が、すでに正規化された空でない区切りかどうかを調べる。
    /// </summary>
    /// <param name="segment">調べる文字列。</param>
    /// <returns>正規化済みで空でなければ true。</returns>
    public static bool IsNormalizedSegment([NotNullWhen(true)] string? segment) =>
        !string.IsNullOrEmpty(segment) && NormalizeSegment(segment) == segment;

    private static bool IsAllowed(char c) => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '.' or '_' or '-';
}

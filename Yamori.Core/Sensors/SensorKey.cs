using System.Diagnostics.CodeAnalysis;

namespace Yamori.Core.Sensors;

/// <summary>
/// センサーを取得元に関係なく識別するキー。文字列表記は <c>{hardwareKind}/{hardwareId}/{sensorType}/{name}</c>（例：<c>cpu/0/temperature/package</c>）。
/// </summary>
/// <remarks>
/// <para>取得元（Provider）の名前はキーに含めない。意味が完全に同じ値だけを同じキーにする
/// （例：ACPI の温度ゾーンと CPU パッケージの温度は別のキー、性能の割合から推定したクロックと MSR から読んだ実クロックも別のキー）。</para>
/// <para>ハードウェア ID は <see cref="HardwareIds"/> で作る、再起動しても変わらない値を使う。列挙の順番は使わない。</para>
/// </remarks>
public readonly record struct SensorKey
{
    private SensorKey(HardwareKind hardwareKind, string hardwareId, SensorType sensorType, string name)
    {
        HardwareKind = hardwareKind;
        HardwareId = hardwareId;
        SensorType = sensorType;
        Name = name;
    }

    /// <summary>ハードウェアの種類を取得する。</summary>
    public HardwareKind HardwareKind { get; }

    /// <summary>ハードウェアを識別する ID（正規化済み）を取得する。</summary>
    public string HardwareId { get; }

    /// <summary>量の種類を取得する。</summary>
    public SensorType SensorType { get; }

    /// <summary>ハードウェアの中でセンサーを区別する名前（正規化済み）を取得する。</summary>
    public string Name { get; }

    /// <summary>
    /// 各区切りを指定してキーを作る。ハードウェア ID と名前は <see cref="SensorKeyTokens.NormalizeSegment"/> で正規化する。
    /// </summary>
    /// <param name="hardwareKind">ハードウェアの種類。</param>
    /// <param name="hardwareId">ハードウェアを識別する ID。</param>
    /// <param name="sensorType">量の種類。</param>
    /// <param name="name">センサーの名前。</param>
    /// <returns>新しいキー。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hardwareId"/> か <paramref name="name"/> が null。</exception>
    /// <exception cref="ArgumentException">正規化すると空になる。</exception>
    /// <exception cref="ArgumentOutOfRangeException">種類が定義されていない値。</exception>
    public static SensorKey Create(HardwareKind hardwareKind, string hardwareId, SensorType sensorType, string name)
    {
        // 種類が定義済みかどうかを、表記の取得で確かめる。
        _ = SensorKeyTokens.ToToken(hardwareKind);
        _ = SensorKeyTokens.ToToken(sensorType);

        return new SensorKey(
            hardwareKind,
            NormalizeOrThrow(hardwareId, nameof(hardwareId)),
            sensorType,
            NormalizeOrThrow(name, nameof(name)));
    }

    /// <summary>
    /// 文字列表記からキーを読み取る。正規化済みの表記だけを受け付ける。
    /// </summary>
    /// <param name="text">文字列表記。</param>
    /// <returns>読み取ったキー。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> が null。</exception>
    /// <exception cref="FormatException">表記が正しくない。</exception>
    public static SensorKey Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return TryParse(text, out var key)
            ? key
            : throw new FormatException($"センサーキーの形式が正しくありません: '{text}'");
    }

    /// <summary>
    /// 文字列表記からキーを読み取る。正規化済みの表記だけを受け付ける。
    /// </summary>
    /// <param name="text">文字列表記。</param>
    /// <param name="key">読み取ったキー。</param>
    /// <returns>読み取れたら true。</returns>
    public static bool TryParse([NotNullWhen(true)] string? text, out SensorKey key)
    {
        key = default;
        if (text is null)
        {
            return false;
        }

        var parts = text.Split(SensorKeyTokens.Separator);
        if (parts.Length != 4
            || !SensorKeyTokens.TryParseHardwareKind(parts[0], out var kind)
            || !SensorKeyTokens.IsNormalizedSegment(parts[1])
            || !SensorKeyTokens.TryParseSensorType(parts[2], out var type)
            || !SensorKeyTokens.IsNormalizedSegment(parts[3]))
        {
            return false;
        }

        key = new SensorKey(kind, parts[1], type, parts[3]);
        return true;
    }

    /// <summary>
    /// 文字列表記を取得する。
    /// </summary>
    /// <returns><c>{hardwareKind}/{hardwareId}/{sensorType}/{name}</c> の形の文字列。</returns>
    public override string ToString() =>
        HardwareId is null
            ? string.Empty
            : string.Join(
                SensorKeyTokens.Separator,
                SensorKeyTokens.ToToken(HardwareKind),
                HardwareId,
                SensorKeyTokens.ToToken(SensorType),
                Name);

    private static string NormalizeOrThrow(string value, string paramName)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        var normalized = SensorKeyTokens.NormalizeSegment(value);
        return normalized.Length > 0
            ? normalized
            : throw new ArgumentException("キーに使える文字が含まれていません。", paramName);
    }
}

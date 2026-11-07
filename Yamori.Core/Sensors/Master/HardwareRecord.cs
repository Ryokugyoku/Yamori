namespace Yamori.Core.Sensors.Master;

/// <summary>
/// マスタの <c>hardware</c> の 1 行。
/// </summary>
/// <param name="HardwareId">行の番号。</param>
/// <param name="StableKey">ハードウェアを識別するキー（<c>{hardwareKind}/{hardwareId}</c>。例：<c>gpu/3f9a12c4</c>）。</param>
/// <param name="Kind">ハードウェアの種類。</param>
/// <param name="Name">表示名。</param>
/// <param name="FirstSeenUtc">初めて見つけた時刻。</param>
/// <param name="LastSeenUtc">最後に見つけた時刻。</param>
public sealed record HardwareRecord(
    long HardwareId,
    string StableKey,
    HardwareKind Kind,
    string Name,
    DateTimeOffset FirstSeenUtc,
    DateTimeOffset LastSeenUtc)
{
    /// <summary>
    /// センサーのキーから、所属するハードウェアのキーを作る。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <returns><c>{hardwareKind}/{hardwareId}</c> の形の文字列。</returns>
    public static string StableKeyOf(SensorKey key) =>
        SensorKeyTokens.ToToken(key.HardwareKind) + SensorKeyTokens.Separator + key.HardwareId;
}

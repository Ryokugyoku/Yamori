namespace Yamori.Core.Sensors;

/// <summary>
/// センサーの値の単位。値はすべてこの基準単位に正規化して扱い、表示のときに読みやすい単位へ変える。
/// </summary>
/// <remarks>
/// IPC で数値として送るため、既存の値の番号は変えないこと。追加は末尾に行う。
/// </remarks>
public enum SensorUnit
{
    /// <summary>パーセント（0〜100。100 を超えることもある）。</summary>
    Percent = 0,

    /// <summary>ヘルツ。</summary>
    Hertz = 1,

    /// <summary>摂氏温度。</summary>
    Celsius = 2,

    /// <summary>ワット。</summary>
    Watt = 3,

    /// <summary>ボルト。</summary>
    Volt = 4,

    /// <summary>アンペア。</summary>
    Ampere = 5,

    /// <summary>毎分回転数。</summary>
    Rpm = 6,

    /// <summary>バイト。</summary>
    Byte = 7,

    /// <summary>バイト毎秒。</summary>
    BytePerSecond = 8,

    /// <summary>毎秒の回数。</summary>
    PerSecond = 9,

    /// <summary>個数（単位なし）。</summary>
    Count = 10,

    /// <summary>秒。</summary>
    Second = 11,

    /// <summary>ワット時。</summary>
    WattHour = 12,
}

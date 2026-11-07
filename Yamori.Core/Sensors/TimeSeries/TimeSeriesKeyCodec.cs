using System.Buffers.Binary;

namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 時系列のキー（<c>sensor_id</c> の uint32 ビッグエンディアン ＋ <c>ts_ms</c> の int64 ビッグエンディアン、計 12 バイト）を作る・読む。
/// </summary>
/// <remarks>
/// バイト列の辞書順が「センサーごと、時刻の順」になるため、センサー単位の時刻範囲をプレフィックスと範囲の検索で読める。
/// 時刻は 0 以上（1970 年以降）だけを受け付ける（負の値はビッグエンディアンの辞書順が時刻の順にならないため）。
/// </remarks>
public static class TimeSeriesKeyCodec
{
    /// <summary>キーの長さ（バイト）。</summary>
    public const int KeyLength = 12;

    /// <summary>センサー番号の部分（プレフィックス）の長さ（バイト）。</summary>
    public const int PrefixLength = 4;

    /// <summary>
    /// キーを作る。
    /// </summary>
    /// <param name="sensorId">センサーの番号。</param>
    /// <param name="timestampMs">時刻（UTC の Unix ミリ秒）。</param>
    /// <returns>12 バイトのキー。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timestampMs"/> が負の値。</exception>
    public static byte[] Encode(uint sensorId, long timestampMs)
    {
        var key = new byte[KeyLength];
        Encode(sensorId, timestampMs, key);
        return key;
    }

    /// <summary>
    /// キーを指定した領域に書き込む。
    /// </summary>
    /// <param name="sensorId">センサーの番号。</param>
    /// <param name="timestampMs">時刻（UTC の Unix ミリ秒）。</param>
    /// <param name="destination">書き込み先。<see cref="KeyLength"/> バイト以上。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timestampMs"/> が負の値。</exception>
    /// <exception cref="ArgumentException"><paramref name="destination"/> が短い。</exception>
    public static void Encode(uint sensorId, long timestampMs, Span<byte> destination)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(timestampMs);
        if (destination.Length < KeyLength)
        {
            throw new ArgumentException("書き込み先が短すぎます。", nameof(destination));
        }

        BinaryPrimitives.WriteUInt32BigEndian(destination, sensorId);
        BinaryPrimitives.WriteInt64BigEndian(destination[PrefixLength..], timestampMs);
    }

    /// <summary>
    /// キーを読む。
    /// </summary>
    /// <param name="key">12 バイトのキー。</param>
    /// <returns>センサーの番号と時刻。</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> の長さが違う。</exception>
    public static (uint SensorId, long TimestampMs) Decode(ReadOnlySpan<byte> key)
    {
        if (key.Length != KeyLength)
        {
            throw new ArgumentException($"キーの長さは {KeyLength} バイトです。", nameof(key));
        }

        return (BinaryPrimitives.ReadUInt32BigEndian(key), BinaryPrimitives.ReadInt64BigEndian(key[PrefixLength..]));
    }

    /// <summary>
    /// センサー 1 つの、時刻の範囲 [<paramref name="fromMs"/>, <paramref name="toMsExclusive"/>) を表すキーの範囲を作る。
    /// </summary>
    /// <param name="sensorId">センサーの番号。</param>
    /// <param name="fromMs">範囲の始まり（含む）。</param>
    /// <param name="toMsExclusive">範囲の終わり（含まない）。</param>
    /// <returns>始まりのキー（含む）と終わりのキー（含まない）。</returns>
    /// <exception cref="ArgumentOutOfRangeException">時刻が負の値か、終わりが始まりより前。</exception>
    public static (byte[] Start, byte[] End) Range(uint sensorId, long fromMs, long toMsExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(toMsExclusive, fromMs);
        return (Encode(sensorId, fromMs), Encode(sensorId, toMsExclusive));
    }
}

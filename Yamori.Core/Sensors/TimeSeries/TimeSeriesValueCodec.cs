using System.Buffers.Binary;

namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 時系列の値を作る・読む。生データは <c>double</c>（8 バイト）＋ <c>source_id</c>（uint16、2 バイト）、
/// 集計は min・max・avg（<c>double</c> × 3）＋ count（uint32）。数値はすべてリトルエンディアン。
/// </summary>
public static class TimeSeriesValueCodec
{
    /// <summary>生データの値の長さ（バイト）。</summary>
    public const int RawLength = 10;

    /// <summary>集計の値の長さ（バイト）。</summary>
    public const int RollupLength = 28;

    /// <summary>
    /// 生データの値を作る。
    /// </summary>
    /// <param name="value">値。</param>
    /// <param name="sourceId">取得元の番号。</param>
    /// <returns>10 バイトの値。</returns>
    public static byte[] EncodeRaw(double value, ushort sourceId)
    {
        var bytes = new byte[RawLength];
        BinaryPrimitives.WriteDoubleLittleEndian(bytes, value);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(8), sourceId);
        return bytes;
    }

    /// <summary>
    /// 生データの値を読む。
    /// </summary>
    /// <param name="bytes">10 バイトの値。</param>
    /// <returns>値と取得元の番号。</returns>
    /// <exception cref="ArgumentException">長さが違う。</exception>
    public static (double Value, ushort SourceId) DecodeRaw(ReadOnlySpan<byte> bytes)
    {
        RequireLength(bytes, RawLength);
        return (BinaryPrimitives.ReadDoubleLittleEndian(bytes), BinaryPrimitives.ReadUInt16LittleEndian(bytes[8..]));
    }

    /// <summary>
    /// 集計の値を作る。
    /// </summary>
    /// <param name="point">集計の区間。</param>
    /// <returns>28 バイトの値。</returns>
    public static byte[] EncodeRollup(RollupPoint point)
    {
        var bytes = new byte[RollupLength];
        BinaryPrimitives.WriteDoubleLittleEndian(bytes, point.Min);
        BinaryPrimitives.WriteDoubleLittleEndian(bytes.AsSpan(8), point.Max);
        BinaryPrimitives.WriteDoubleLittleEndian(bytes.AsSpan(16), point.Average);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24), point.Count);
        return bytes;
    }

    /// <summary>
    /// 集計の値を読む。
    /// </summary>
    /// <param name="sensorId">センサーの番号（キーから読んだもの）。</param>
    /// <param name="bucketStartMs">区間の始まり（キーから読んだもの）。</param>
    /// <param name="bytes">28 バイトの値。</param>
    /// <returns>集計の区間。</returns>
    /// <exception cref="ArgumentException">長さが違う。</exception>
    public static RollupPoint DecodeRollup(uint sensorId, long bucketStartMs, ReadOnlySpan<byte> bytes)
    {
        RequireLength(bytes, RollupLength);
        return new RollupPoint(
            sensorId,
            bucketStartMs,
            BinaryPrimitives.ReadDoubleLittleEndian(bytes),
            BinaryPrimitives.ReadDoubleLittleEndian(bytes[8..]),
            BinaryPrimitives.ReadDoubleLittleEndian(bytes[16..]),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[24..]));
    }

    private static void RequireLength(ReadOnlySpan<byte> bytes, int length)
    {
        if (bytes.Length != length)
        {
            throw new ArgumentException($"値の長さは {length} バイトです。", nameof(bytes));
        }
    }
}

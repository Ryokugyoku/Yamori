using System.Buffers.Binary;
using System.Text;
using RocksDbSharp;

namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// RocksDB（組み込みの KV ストア）による <see cref="ITimeSeriesStore"/>。解像度ごとに Column Family を分け、
/// 書き込みはすべて WriteBatch で 1 回にまとめる。WAL は有効のまま（既定）で、異常終了しても書き込み済みのデータは残る。
/// </summary>
/// <remarks>
/// 1 つのフォルダーを同時に開けるのは 1 つのプロセスだけ（シングルライター）。スレッドセーフ。
/// </remarks>
public sealed class RocksDbTimeSeriesStore : ITimeSeriesStore, IDisposable
{
    private const string WatermarkKeyPrefix = "watermark:";

    private readonly RocksDb _db;
    private readonly ColumnFamilyHandle _default;
    private readonly IReadOnlyDictionary<TimeSeriesResolution, ColumnFamilyHandle> _families;

    private RocksDbTimeSeriesStore(RocksDb db)
    {
        _db = db;
        _default = db.GetDefaultColumnFamily();
        _families = Enum.GetValues<TimeSeriesResolution>()
            .ToDictionary(r => r, r => db.GetColumnFamily(TimeSeriesResolutions.ColumnFamilyName(r)));
    }

    /// <summary>
    /// フォルダーを指定してストアを開く。なければ作る。
    /// </summary>
    /// <param name="directory">データを置くフォルダー。</param>
    /// <returns>開いたストア。使い終わったら破棄すること。</returns>
    /// <exception cref="ArgumentException"><paramref name="directory"/> が null か空。</exception>
    /// <exception cref="RocksDbException">開けなかった（ほかのプロセスが開いている、壊れているなど）。</exception>
    public static RocksDbTimeSeriesStore Open(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        Directory.CreateDirectory(directory);

        var options = new DbOptions()
            .SetCreateIfMissing(true)
            .SetCreateMissingColumnFamilies(true);

        var families = new ColumnFamilies();
        foreach (var resolution in Enum.GetValues<TimeSeriesResolution>())
        {
            families.Add(
                TimeSeriesResolutions.ColumnFamilyName(resolution),
                new ColumnFamilyOptions().SetCompression(Compression.Lz4));
        }

        return new RocksDbTimeSeriesStore(RocksDb.Open(options, directory, families));
    }

    /// <inheritdoc />
    public void WriteRaw(IReadOnlyCollection<TimeSeriesPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            return;
        }

        var family = _families[TimeSeriesResolution.Raw];
        using var batch = new WriteBatch();
        foreach (var point in points)
        {
            batch.Put(
                TimeSeriesKeyCodec.Encode(point.SensorId, point.TimestampMs),
                TimeSeriesValueCodec.EncodeRaw(point.Value, point.SourceId),
                family);
        }

        _db.Write(batch);
    }

    /// <inheritdoc />
    public void WriteRollups(TimeSeriesResolution resolution, IReadOnlyCollection<RollupPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        var family = RollupFamily(resolution);
        if (points.Count == 0)
        {
            return;
        }

        using var batch = new WriteBatch();
        foreach (var point in points)
        {
            batch.Put(TimeSeriesKeyCodec.Encode(point.SensorId, point.BucketStartMs), TimeSeriesValueCodec.EncodeRollup(point), family);
        }

        _db.Write(batch);
    }

    /// <inheritdoc />
    public IReadOnlyList<TimeSeriesPoint> ReadRaw(uint sensorId, long fromMs, long toMsExclusive) =>
        Scan(_families[TimeSeriesResolution.Raw], sensorId, fromMs, toMsExclusive, (id, ts, value) =>
        {
            var (v, source) = TimeSeriesValueCodec.DecodeRaw(value);
            return new TimeSeriesPoint(id, ts, v, source);
        });

    /// <inheritdoc />
    public IReadOnlyList<RollupPoint> ReadRollups(TimeSeriesResolution resolution, uint sensorId, long fromMs, long toMsExclusive) =>
        Scan(RollupFamily(resolution), sensorId, fromMs, toMsExclusive, (id, ts, value) => TimeSeriesValueCodec.DecodeRollup(id, ts, value));

    /// <inheritdoc />
    public void DeleteBefore(TimeSeriesResolution resolution, IReadOnlyCollection<uint> sensorIds, long cutoffMs)
    {
        ArgumentNullException.ThrowIfNull(sensorIds);
        if (sensorIds.Count == 0 || cutoffMs <= 0)
        {
            return;
        }

        var family = _families[resolution];
        using var batch = new WriteBatch();
        foreach (var sensorId in sensorIds)
        {
            var (start, end) = TimeSeriesKeyCodec.Range(sensorId, 0, cutoffMs);
            batch.DeleteRange(start, (ulong)start.Length, end, (ulong)end.Length, family);
        }

        _db.Write(batch);
    }

    /// <inheritdoc />
    public long? GetRollupWatermark(TimeSeriesResolution resolution)
    {
        var value = _db.Get(WatermarkKey(resolution), _default);
        return value is { Length: 8 } ? BinaryPrimitives.ReadInt64LittleEndian(value) : null;
    }

    /// <inheritdoc />
    public void SetRollupWatermark(TimeSeriesResolution resolution, long watermarkMs)
    {
        var value = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(value, watermarkMs);
        _db.Put(WatermarkKey(resolution), value, _default);
    }

    /// <summary>
    /// データベースを閉じる。
    /// </summary>
    public void Dispose() => _db.Dispose();

    private static byte[] WatermarkKey(TimeSeriesResolution resolution) =>
        Encoding.ASCII.GetBytes(WatermarkKeyPrefix + TimeSeriesResolutions.ColumnFamilyName(resolution));

    private ColumnFamilyHandle RollupFamily(TimeSeriesResolution resolution)
    {
        _ = TimeSeriesResolutions.BucketMs(resolution);
        return _families[resolution];
    }

    private List<T> Scan<T>(ColumnFamilyHandle family, uint sensorId, long fromMs, long toMsExclusive, Func<uint, long, byte[], T> decode)
    {
        var result = new List<T>();
        if (toMsExclusive <= fromMs)
        {
            return result;
        }

        var (start, end) = TimeSeriesKeyCodec.Range(sensorId, Math.Max(0, fromMs), toMsExclusive);
        var readOptions = new ReadOptions().SetIterateUpperBound(end);
        using var iterator = _db.NewIterator(family, readOptions);
        for (iterator.Seek(start); iterator.Valid(); iterator.Next())
        {
            var (id, ts) = TimeSeriesKeyCodec.Decode(iterator.Key());
            result.Add(decode(id, ts, iterator.Value()));
        }

        // 上限のキーは読み取りの間、ネイティブ側から参照される。
        GC.KeepAlive(end);
        return result;
    }
}

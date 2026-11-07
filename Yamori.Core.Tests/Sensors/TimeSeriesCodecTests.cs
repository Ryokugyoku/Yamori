using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Tests.Sensors;

public class TimeSeriesCodecTests
{
    [Fact]
    public void KeyIsSensorIdThenTimestampInBigEndian()
    {
        var key = TimeSeriesKeyCodec.Encode(0x01020304, 0x0A0B0C0D0E0F1011);

        Assert.Equal(
            new byte[] { 0x01, 0x02, 0x03, 0x04, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11 },
            key);
    }

    [Fact]
    public void KeyRoundTrips()
    {
        var key = TimeSeriesKeyCodec.Encode(42, 1_762_473_600_000);

        Assert.Equal((42u, 1_762_473_600_000L), TimeSeriesKeyCodec.Decode(key));
    }

    [Fact]
    public void ByteOrderMatchesSensorThenTimeOrder()
    {
        // RocksDB はキーをバイト列の辞書順に並べる。その順が「センサーごと、時刻の順」になっていること。
        (uint Id, long Ts)[] expected =
        [
            (1, 0), (1, 255), (1, 256), (1, 1_762_473_600_000), (1, long.MaxValue),
            (2, 0), (2, 1), (256, 0), (uint.MaxValue, 5),
        ];

        var sorted = expected
            .Select(x => TimeSeriesKeyCodec.Encode(x.Id, x.Ts))
            .OrderBy(k => k, ByteArrayComparer.Instance)
            .Select(k => TimeSeriesKeyCodec.Decode(k))
            .ToArray();

        Assert.Equal(expected.Select(x => (x.Id, x.Ts)), sorted);
    }

    [Fact]
    public void RangeCoversOnlyOneSensor()
    {
        var (start, end) = TimeSeriesKeyCodec.Range(7, 1000, 2000);

        Assert.Equal(TimeSeriesKeyCodec.Encode(7, 1000), start);
        Assert.Equal(TimeSeriesKeyCodec.Encode(7, 2000), end);
        Assert.True(ByteArrayComparer.Instance.Compare(TimeSeriesKeyCodec.Encode(6, long.MaxValue), start) < 0);
        Assert.True(ByteArrayComparer.Instance.Compare(TimeSeriesKeyCodec.Encode(8, 0), end) > 0);
    }

    [Fact]
    public void NegativeTimestampsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TimeSeriesKeyCodec.Encode(1, -1));
        Assert.Throws<ArgumentException>(() => TimeSeriesKeyCodec.Decode(new byte[11]));
    }

    [Fact]
    public void ValuesRoundTrip()
    {
        var raw = TimeSeriesValueCodec.EncodeRaw(-12.5, 3);
        Assert.Equal(TimeSeriesValueCodec.RawLength, raw.Length);
        Assert.Equal((-12.5, (ushort)3), TimeSeriesValueCodec.DecodeRaw(raw));

        var rollup = new RollupPoint(9, 60_000, 1.5, 9.25, 4.125, 60);
        var bytes = TimeSeriesValueCodec.EncodeRollup(rollup);
        Assert.Equal(TimeSeriesValueCodec.RollupLength, bytes.Length);
        Assert.Equal(rollup, TimeSeriesValueCodec.DecodeRollup(9, 60_000, bytes));
    }

    private sealed class ByteArrayComparer : IComparer<byte[]>
    {
        public static ByteArrayComparer Instance { get; } = new();

        public int Compare(byte[]? x, byte[]? y) => x.AsSpan().SequenceCompareTo(y);
    }
}

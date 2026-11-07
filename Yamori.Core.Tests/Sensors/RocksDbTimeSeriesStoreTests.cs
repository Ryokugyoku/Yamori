using Yamori.Core.Sensors.TimeSeries;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Sensors;

public sealed class RocksDbTimeSeriesStoreTests : IDisposable
{
    private readonly TempDirectory _directory = new();

    public void Dispose() => _directory.Dispose();

    [Fact]
    public void ReadsOnlyTheRequestedSensorAndTimeRangeInOrder()
    {
        using var store = RocksDbTimeSeriesStore.Open(_directory.Path);
        store.WriteRaw(
        [
            new TimeSeriesPoint(2, 3000, 30, 1),
            new TimeSeriesPoint(1, 1000, 10, 1),
            new TimeSeriesPoint(1, 2000, 20, 2),
            new TimeSeriesPoint(1, 3000, 30, 1),
            new TimeSeriesPoint(3, 1500, 99, 1),
        ]);

        var points = store.ReadRaw(1, 1000, 3000);

        Assert.Equal([1000L, 2000L], points.Select(p => p.TimestampMs));
        Assert.Equal((ushort)2, points[1].SourceId);
        Assert.Empty(store.ReadRaw(1, 3001, 9000));
    }

    [Fact]
    public void DeleteBeforeRemovesOnlyOlderDataOfListedSensors()
    {
        using var store = RocksDbTimeSeriesStore.Open(_directory.Path);
        store.WriteRaw([new(1, 100, 1, 1), new(1, 200, 2, 1), new(1, 300, 3, 1), new(2, 100, 4, 1)]);
        store.WriteRollups(TimeSeriesResolution.OneMinute, [new RollupPoint(1, 0, 1, 1, 1, 1)]);

        store.DeleteBefore(TimeSeriesResolution.Raw, [1], 300);

        Assert.Equal([300L], store.ReadRaw(1, 0, long.MaxValue).Select(p => p.TimestampMs));
        Assert.Single(store.ReadRaw(2, 0, long.MaxValue));
        Assert.Single(store.ReadRollups(TimeSeriesResolution.OneMinute, 1, 0, long.MaxValue));
    }

    [Fact]
    public void DataAndWatermarksSurviveReopen()
    {
        using (var store = RocksDbTimeSeriesStore.Open(_directory.Path))
        {
            store.WriteRaw([new(5, 60_000, 42, 1)]);
            store.WriteRollups(TimeSeriesResolution.OneHour, [new RollupPoint(5, 0, 1, 2, 1.5, 120)]);
            store.SetRollupWatermark(TimeSeriesResolution.OneMinute, 120_000);
        }

        using var reopened = RocksDbTimeSeriesStore.Open(_directory.Path);

        Assert.Equal(42, Assert.Single(reopened.ReadRaw(5, 0, long.MaxValue)).Value);
        Assert.Equal(120u, Assert.Single(reopened.ReadRollups(TimeSeriesResolution.OneHour, 5, 0, long.MaxValue)).Count);
        Assert.Equal(120_000, reopened.GetRollupWatermark(TimeSeriesResolution.OneMinute));
        Assert.Null(reopened.GetRollupWatermark(TimeSeriesResolution.OneHour));
    }

    [Fact]
    public void SecondOpenOfTheSameFolderFails()
    {
        // シングルライター：同じフォルダーを 2 つ同時には開けない。
        using var first = RocksDbTimeSeriesStore.Open(_directory.Path);

        Assert.ThrowsAny<Exception>(() => RocksDbTimeSeriesStore.Open(_directory.Path).Dispose());
    }

    [Fact]
    public void RawCannotBeUsedAsRollupResolution()
    {
        using var store = RocksDbTimeSeriesStore.Open(_directory.Path);

        Assert.Throws<ArgumentOutOfRangeException>(() => store.WriteRollups(TimeSeriesResolution.Raw, []));
    }
}

using Yamori.Core.Sensors.TimeSeries;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Sensors;

public class RollupAndRetentionTests
{
    private const long Minute = TimeSeriesResolutions.OneMinuteMs;
    private const long Hour = TimeSeriesResolutions.OneHourMs;
    private const long Day = 24 * Hour;
    private static readonly long Now = new DateTimeOffset(2026, 11, 7, 12, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

    [Fact]
    public void FromRawComputesMinMaxAverageCountPerBucket()
    {
        var rollups = RollupCalculator.FromRaw(
            [new(1, 0, 10, 1), new(1, 30_000, 20, 1), new(1, Minute, 5, 1), new(2, 0, 7, 1)],
            Minute);

        Assert.Equal(
            [new RollupPoint(1, 0, 10, 20, 15, 2), new RollupPoint(1, Minute, 5, 5, 5, 1), new RollupPoint(2, 0, 7, 7, 7, 1)],
            rollups);
    }

    [Fact]
    public void CombineWeightsAverageByCount()
    {
        var hour = Assert.Single(RollupCalculator.Combine(
            [new RollupPoint(1, 0, 0, 10, 10, 1), new RollupPoint(1, Minute, 2, 4, 2, 3), new RollupPoint(1, 2 * Minute, 9, 9, 9, 0)],
            Hour));

        Assert.Equal(new RollupPoint(1, 0, 0, 10, 4, 4), hour);
    }

    [Fact]
    public void RollupJobOnlyAggregatesFinishedBucketsAndAdvancesWatermark()
    {
        var store = new InMemoryTimeSeriesStore();
        var start = Now - 5 * Minute;
        // 最後の点は、まだ終わっていない区間（今の分）にある。
        store.WriteRaw([new(1, start, 10, 1), new(1, start + 30_000, 30, 1), new(1, Now + 10_000, 99, 1)]);
        var job = new RollupJob(store, RetentionPolicy.Default, TimeSpan.Zero);

        job.Run(Now, [1]);

        var minutes = store.ReadRollups(TimeSeriesResolution.OneMinute, 1, 0, long.MaxValue);
        var first = Assert.Single(minutes);
        Assert.Equal(new RollupPoint(1, start, 10, 30, 20, 2), first);
        Assert.Equal(Now, store.GetRollupWatermark(TimeSeriesResolution.OneMinute));
    }

    [Fact]
    public void RollupJobIsIdempotentAndContinuesFromTheWatermark()
    {
        var store = new InMemoryTimeSeriesStore();
        var job = new RollupJob(store, RetentionPolicy.Default, TimeSpan.Zero);
        store.WriteRaw([new(1, Now - 2 * Minute, 10, 1)]);
        job.Run(Now, [1]);

        // 次の分のデータが来てから、もう一度動かす。
        store.WriteRaw([new(1, Now + 10_000, 50, 1)]);
        job.Run(Now + Minute, [1]);
        job.Run(Now + Minute, [1]);

        Assert.Equal(
            [Now - 2 * Minute, Now],
            store.ReadRollups(TimeSeriesResolution.OneMinute, 1, 0, long.MaxValue).Select(r => r.BucketStartMs));
    }

    [Fact]
    public void HourlyRollupWaitsForMinuteRollups()
    {
        var store = new InMemoryTimeSeriesStore();
        var job = new RollupJob(store, RetentionPolicy.Default, TimeSpan.Zero);
        var hourStart = Now - Hour;
        store.WriteRaw([new(1, hourStart, 10, 1), new(1, hourStart + 30 * Minute, 20, 1)]);

        job.Run(Now, [1]);

        var hour = Assert.Single(store.ReadRollups(TimeSeriesResolution.OneHour, 1, 0, long.MaxValue));
        Assert.Equal(new RollupPoint(1, hourStart, 10, 20, 15, 2), hour);
        Assert.Equal(Now, store.GetRollupWatermark(TimeSeriesResolution.OneHour));
    }

    [Fact]
    public void SettleDelayKeepsTheLatestBucketOpen()
    {
        var store = new InMemoryTimeSeriesStore();
        var job = new RollupJob(store, RetentionPolicy.Default, TimeSpan.FromSeconds(15));
        store.WriteRaw([new(1, Now - Minute, 10, 1)]);

        // 区間が終わってから 10 秒しか経っていない。
        job.Run(Now + 10_000, [1]);

        Assert.Empty(store.ReadRollups(TimeSeriesResolution.OneMinute, 1, 0, long.MaxValue));
        Assert.Equal(Now - Minute, store.GetRollupWatermark(TimeSeriesResolution.OneMinute));
    }

    [Fact]
    public void RetentionUsesDefaultPeriodsAndKeepsHourlyForever()
    {
        var store = new InMemoryTimeSeriesStore();
        var job = new RetentionJob(store, RetentionPolicy.Default);

        var cutoffs = job.Run(Now, [1, 2]);

        Assert.Equal(Now - 7 * Day, cutoffs[TimeSeriesResolution.Raw]);
        Assert.Equal(Now - 90 * Day, cutoffs[TimeSeriesResolution.OneMinute]);
        Assert.False(cutoffs.ContainsKey(TimeSeriesResolution.OneHour));
        Assert.DoesNotContain(store.Deletes, d => d.Resolution == TimeSeriesResolution.OneHour);
        Assert.All(store.Deletes, d => Assert.Equal([1u, 2u], d.SensorIds));
    }

    [Fact]
    public void RetentionDeletesOnlyDataOlderThanTheCutoff()
    {
        var store = new InMemoryTimeSeriesStore();
        store.WriteRaw([new(1, Now - 8 * Day, 1, 1), new(1, Now - 7 * Day, 2, 1), new(1, Now - Day, 3, 1)]);
        store.WriteRollups(TimeSeriesResolution.OneMinute, [new RollupPoint(1, Now - 91 * Day, 1, 1, 1, 1), new RollupPoint(1, Now - 89 * Day, 1, 1, 1, 1)]);
        store.WriteRollups(TimeSeriesResolution.OneHour, [new RollupPoint(1, Now - 400 * Day, 1, 1, 1, 1)]);

        new RetentionJob(store, RetentionPolicy.Default).Run(Now, [1]);

        Assert.Equal([2.0, 3.0], store.ReadRaw(1, 0, long.MaxValue).Select(p => p.Value));
        Assert.Equal([Now - 89 * Day], store.ReadRollups(TimeSeriesResolution.OneMinute, 1, 0, long.MaxValue).Select(r => r.BucketStartMs));
        Assert.Single(store.ReadRollups(TimeSeriesResolution.OneHour, 1, 0, long.MaxValue));
    }

    [Fact]
    public void RetentionWorksAgainstRocksDb()
    {
        using var directory = new TempDirectory();
        using var store = RocksDbTimeSeriesStore.Open(directory.Path);
        store.WriteRaw([new(1, Now - 8 * Day, 1, 1), new(1, Now - Day, 2, 1), new(2, Now - 8 * Day, 3, 1)]);

        new RetentionJob(store, RetentionPolicy.Default).Run(Now, [1, 2]);

        Assert.Equal([2.0], store.ReadRaw(1, 0, long.MaxValue).Select(p => p.Value));
        Assert.Empty(store.ReadRaw(2, 0, long.MaxValue));
    }

    [Fact]
    public void CutoffNeverGoesBelowZero()
    {
        Assert.Equal(0, RetentionJob.Cutoff(1000, TimeSpan.FromDays(7)));
    }
}

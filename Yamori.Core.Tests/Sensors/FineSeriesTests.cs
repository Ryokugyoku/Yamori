using Microsoft.Data.Sqlite;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Master;
using Yamori.Core.Sensors.Persistence;
using Yamori.Core.Sensors.TimeSeries;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Sensors;

public class FineSeriesTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 8, 0, 0, 0, TimeSpan.Zero);
    private static readonly SensorKey CpuLoad = SensorKey.Parse("cpu/0/load/total");
    private static readonly SensorKey ThreadLoad = SensorKey.Parse("cpu/0/load/thread-0");

    [Theory]
    [InlineData("cpu/0/load/total", true)]
    [InlineData("cpu/0/temperature/package", true)]
    [InlineData("cpu/0/clock/average-effective", true)]
    [InlineData("cpu/0/flag/thermal-throttling", true)]
    [InlineData("gpu/3f9a12c4/clock/core", true)]
    [InlineData("thermal-zone/ab12/flag/throttling", true)]
    [InlineData("cpu/0/load/thread-3", false)]
    [InlineData("cpu/0/temperature/core-0", false)]
    [InlineData("cpu/0/temperature/tjmax", false)]
    [InlineData("gpu/3f9a12c4/load/engine-3d", false)]
    [InlineData("memory/0/data/used", false)]
    public void SelectionKeepsOnlyTheSensorsUsedForDetection(string key, bool expected)
    {
        Assert.Equal(expected, new FineSeriesSelection().Includes(SensorKey.Parse(key)));
    }

    [Fact]
    public void SelectionReplacesThePolicyAndKeepsTheSensorId()
    {
        var targets = new Dictionary<SensorKey, SensorLogTarget>
        {
            [CpuLoad] = new(3, SensorLogPolicy.Disabled),
            [ThreadLoad] = new(4, SensorLogPolicy.Disabled),
        };

        var selected = new FineSeriesSelection().Select(targets);

        var target = Assert.Single(selected);
        Assert.Equal(CpuLoad, target.Key);
        Assert.Equal(new SensorLogTarget(3, FineSeriesSelection.Policy), target.Value);
    }

    [Fact]
    public void OneSecondValuesAreKeptForADay()
    {
        Assert.Equal(TimeSpan.FromHours(24), RetentionPolicy.Default.For(TimeSeriesResolution.OneSecond));

        var store = new InMemoryTimeSeriesStore();
        var now = Start.ToUnixTimeMilliseconds();
        store.WritePoints(TimeSeriesResolution.OneSecond, [new(1, now - (25 * 3_600_000L), 1, 1), new(1, now - 3_600_000L, 2, 1)]);

        var cutoffs = new RetentionJob(store, RetentionPolicy.Default).Run(now, [1]);

        Assert.Equal(now - (24 * 3_600_000L), cutoffs[TimeSeriesResolution.OneSecond]);
        Assert.Equal([2.0], store.ReadPoints(TimeSeriesResolution.OneSecond, 1, 0, long.MaxValue).Select(p => p.Value));
    }

    [Fact]
    public void RocksDbKeepsOneSecondValuesInTheirOwnColumnFamily()
    {
        Assert.Equal("fine_1s", TimeSeriesResolutions.ColumnFamilyName(TimeSeriesResolution.OneSecond));
        using var directory = new TempDirectory();
        using (var store = RocksDbTimeSeriesStore.Open(directory.Path))
        {
            store.WritePoints(TimeSeriesResolution.OneSecond, [new(1, 1000, 10, 1), new(1, 2000, 11, 1)]);
            store.WriteRaw([new(1, 1000, 99, 1)]);
        }

        // 列ファミリーが増えたフォルダーを開き直せること。
        using var reopened = RocksDbTimeSeriesStore.Open(directory.Path);
        Assert.Equal([10.0, 11.0], reopened.ReadPoints(TimeSeriesResolution.OneSecond, 1, 0, long.MaxValue).Select(p => p.Value));
        Assert.Equal([99.0], reopened.ReadRaw(1, 0, long.MaxValue).Select(p => p.Value));
        Assert.Throws<ArgumentOutOfRangeException>(() => reopened.WritePoints(TimeSeriesResolution.OneMinute, []));
    }

    [Fact]
    public void RecorderRejectsRollupResolutions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeSeriesRecorder(new InMemoryTimeSeriesStore(), new LogDecisionFilter(), TimeSeriesResolution.OneHour));
    }

    [Fact]
    public async Task PersistenceRecordsSelectedSensorsEverySecondApartFromTheLogPolicy()
    {
        SqliteConnection.ClearAllPools();
        using var master = MasterDatabase.Open(":memory:");
        var store = new InMemoryTimeSeriesStore();
        var sensors = new SqliteSensorRepository(master);
        var retention = RetentionPolicy.Default;
        var persistence = new SensorPersistence(
            store,
            new TimeSeriesRecorder(store, new LogDecisionFilter()),
            new TimeSeriesRecorder(store, new LogDecisionFilter(), TimeSeriesResolution.OneSecond),
            new FineSeriesSelection(),
            new SensorCatalogSynchronizer(new SqliteHardwareRepository(master), sensors, new SqliteSourceRepository(master)),
            sensors,
            new SqliteEventLogRepository(master),
            new RollupJob(store, retention),
            new RetentionJob(store, retention),
            ["win-std"],
            new ManualClock { Now = Start });
        persistence.OnCatalogChanged(
        [
            new SensorDescriptor(CpuLoad, SensorUnit.Percent, "CPU Total", "CPU", "load", "win-std"),
            new SensorDescriptor(ThreadLoad, SensorUnit.Percent, "Thread #0", "CPU", "threads", "win-std"),
        ]);

        for (var second = 0; second < 5; second++)
        {
            var at = Start.AddSeconds(second);
            persistence.OnReadings([SensorReading.Ok(CpuLoad, 50, at, "win-std"), SensorReading.Ok(ThreadLoad, 50, at, "win-std")]);
        }

        // 止める合図が来ていても、ためた値は書き込む。
        await persistence.RunAsync(new CancellationToken(canceled: true));

        var fine = persistence.GetHistory(CpuLoad, Start, Start.AddMinutes(1), TimeSeriesResolution.OneSecond);
        var raw = persistence.GetHistory(CpuLoad, Start, Start.AddMinutes(1), TimeSeriesResolution.Raw);
        Assert.Equal(5, fine.Count);                // 毎秒、デッドバンドなし
        Assert.Single(raw);                         // 通常の記録は 10 秒ごと・デッドバンドあり
        Assert.Empty(persistence.GetHistory(ThreadLoad, Start, Start.AddMinutes(1), TimeSeriesResolution.OneSecond));
    }
}

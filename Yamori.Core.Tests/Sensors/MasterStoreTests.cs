using Microsoft.Data.Sqlite;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Master;
using Yamori.Core.Sensors.Persistence;
using Yamori.Core.Sensors.TimeSeries;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Sensors;

public sealed class MasterStoreTests : IDisposable
{
    private static readonly DateTimeOffset Seen = new(2026, 11, 7, 0, 0, 0, TimeSpan.Zero);
    private readonly TempDirectory _directory = new();

    public MasterStoreTests() => SqliteConnection.ClearAllPools();

    private string DbPath => Path.Combine(_directory.Path, "master.db");

    public void Dispose() => _directory.Dispose();

    [Fact]
    public void OpenMigratesToTheCurrentVersionInWalMode()
    {
        using var db = MasterDatabase.Open(DbPath);

        Assert.Equal(MasterSchema.CurrentVersion, db.SchemaVersion);
        Assert.Equal("wal", db.Execute(c => Scalar(c, "PRAGMA journal_mode;")));
        Assert.Equal(
            ["event_log", "hardware", "sensor", "source"],
            db.Execute(c => Strings(c, "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;")));
    }

    [Fact]
    public void ReopeningDoesNotMigrateAgain()
    {
        using (var db = MasterDatabase.Open(DbPath))
        {
            new SqliteSourceRepository(db).GetOrCreate("win-std");
        }

        using var reopened = MasterDatabase.Open(DbPath);

        Assert.Equal(MasterSchema.CurrentVersion, reopened.SchemaVersion);
        Assert.Equal((ushort)1, new SqliteSourceRepository(reopened).GetAll()["win-std"]);
    }

    [Fact]
    public void NewerSchemaIsRejected()
    {
        using (var connection = new SqliteConnection($"Data Source={DbPath};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA user_version = {MasterSchema.CurrentVersion + 1};";
            command.ExecuteNonQuery();
        }

        Assert.Throws<NotSupportedException>(() => MasterDatabase.Open(DbPath));
    }

    [Fact]
    public void HardwareUpsertKeepsFirstSeenAndUpdatesLastSeen()
    {
        using var db = MasterDatabase.Open(":memory:");
        var repository = new SqliteHardwareRepository(db);

        var first = repository.Upsert("gpu/3f9a12c4", HardwareKind.Gpu, "GPU A", Seen);
        var second = repository.Upsert("gpu/3f9a12c4", HardwareKind.Gpu, "GPU A (renamed)", Seen.AddDays(1));

        Assert.Equal(first.HardwareId, second.HardwareId);
        Assert.Equal(Seen, second.FirstSeenUtc);
        Assert.Equal(Seen.AddDays(1), second.LastSeenUtc);
        Assert.Equal("GPU A (renamed)", Assert.Single(repository.GetAll()).Name);
    }

    [Fact]
    public void SensorIdsAreAssignedOnceAndUserPoliciesSurviveRediscovery()
    {
        using var db = MasterDatabase.Open(":memory:");
        var hardware = new SqliteHardwareRepository(db).Upsert("cpu/0", HardwareKind.Cpu, "CPU", Seen);
        var sensors = new SqliteSensorRepository(db);
        var descriptor = Descriptor("cpu/0/load/total");

        var created = sensors.Ensure(descriptor, hardware.HardwareId, new SensorLogPolicy(true, TimeSpan.FromSeconds(10), 0.5));
        var other = sensors.Ensure(Descriptor("cpu/0/load/user"), hardware.HardwareId, SensorLogPolicy.Disabled);
        sensors.UpdateLogPolicy(descriptor.Key, new SensorLogPolicy(false, TimeSpan.FromSeconds(30), 2));
        var again = sensors.Ensure(descriptor with { DisplayName = "Total (new name)" }, hardware.HardwareId, new SensorLogPolicy(true, TimeSpan.FromSeconds(10), 0.5));

        Assert.NotEqual(created.SensorId, other.SensorId);
        Assert.Equal(created.SensorId, again.SensorId);
        Assert.Equal(new SensorLogPolicy(false, TimeSpan.FromSeconds(30), 2), again.LogPolicy);
        Assert.Equal("Total (new name)", sensors.Find(descriptor.Key)!.Name);
        Assert.Null(sensors.UpdateLogPolicy(SensorKey.Parse("gpu/x/load/core"), SensorLogPolicy.Disabled));
    }

    [Fact]
    public void SourceIdsAreStable()
    {
        using var db = MasterDatabase.Open(":memory:");
        var sources = new SqliteSourceRepository(db);

        var standard = sources.GetOrCreate("win-std");
        var pawnio = sources.GetOrCreate("pawnio");

        Assert.NotEqual(standard, pawnio);
        Assert.Equal(standard, sources.GetOrCreate("win-std"));
    }

    [Fact]
    public void EventLogReturnsNewestFirstWithData()
    {
        using var db = MasterDatabase.Open(":memory:");
        var events = new SqliteEventLogRepository(db);

        events.Append(new EventLogEntry(0, Seen, "app", EventLevel.Information, "started", null));
        var written = events.Append(new EventLogEntry(0, Seen.AddMinutes(1), "helper", EventLevel.Error, "stopped", """{"code":5}"""));

        var recent = events.GetRecent(10);
        Assert.Equal(["stopped", "started"], recent.Select(e => e.Message));
        Assert.Equal(written, recent[0]);
        Assert.Equal("""{"code":5}""", recent[0].DataJson);
        Assert.Null(recent[1].DataJson);
    }

    [Fact]
    public void SynchronizerUpsertsCatalogAndBuildsLogTargets()
    {
        using var db = MasterDatabase.Open(":memory:");
        var sensors = new SqliteSensorRepository(db);
        var synchronizer = new SensorCatalogSynchronizer(new SqliteHardwareRepository(db), sensors, new SqliteSourceRepository(db));
        SensorDescriptor[] catalog =
        [
            Descriptor("cpu/0/load/total"),
            Descriptor("cpu/0/load/thread-0", group: "threads"),
            Descriptor("gpu/3f9a12c4/load/max-engine", hardwareName: "GPU A"),
        ];

        var (targets, sourceIds) = synchronizer.Synchronize(catalog, ["win-std", "pawnio"], Seen);

        Assert.Equal(3, targets.Count);
        Assert.True(targets[catalog[0].Key].Policy.Enabled);
        Assert.False(targets[catalog[1].Key].Policy.Enabled);
        Assert.Equal(["pawnio", "win-std"], sourceIds.Keys.Order());
        Assert.Equal(["cpu/0", "gpu/3f9a12c4"], new SqliteHardwareRepository(db).GetAll().Select(h => h.StableKey));

        // 2 回目は番号を変えない。
        var (again, _) = synchronizer.Synchronize(catalog, ["win-std"], Seen.AddHours(1));
        Assert.Equal(targets[catalog[2].Key].SensorId, again[catalog[2].Key].SensorId);
    }

    private static SensorDescriptor Descriptor(string key, string group = "load", string hardwareName = "CPU") =>
        new(SensorKey.Parse(key), SensorUnit.Percent, key, hardwareName, group, "win-std");

    private static string Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture)!;
    }

    private static List<string> Strings(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }
}

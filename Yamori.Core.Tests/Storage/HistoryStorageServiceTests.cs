using Yamori.Core.Hardware;
using Yamori.Core.Monitoring;
using Yamori.Core.Settings;
using Yamori.Core.Storage;

namespace Yamori.Core.Tests.Storage;

public sealed class HistoryStorageServiceTests : IDisposable
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly string _root = Path.Combine(Path.GetTempPath(), "YamoriTests", Guid.NewGuid().ToString("N"));

    private string LocalDirectory => Path.Combine(_root, "local", "history");

    private string CustomRoot => Path.Combine(_root, "custom");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private HistoryStorageService Create(YamoriSettings settings) =>
        new(
            new HistoryLocationResolver(new OneDriveLocator(_ => null, _ => false), LocalDirectory, "PC"),
            new HistoryMigrator(),
            directory => new CsvUsageHistoryStore(directory, new UsageSampleCsvSerializer()),
            settings);

    private static ComponentUsageSample Sample(int minute, double usage) =>
        new(HardwareComponent.Cpu, T0.AddMinutes(minute), usage);

    [Fact]
    public void ApplyCopiesHistoryAndSwitchesWrites()
    {
        Directory.CreateDirectory(CustomRoot);
        var service = Create(YamoriSettings.Default);
        service.Store.Append([Sample(0, 10)]);

        var changed = new List<HistoryLocation>();
        service.LocationChanged += (_, location) => changed.Add(location);
        var location = service.Apply(new YamoriSettings { StorageLocation = StorageLocationKind.CustomFolder, CustomFolderPath = CustomRoot });
        service.Store.Append([Sample(1, 20)]);

        Assert.Equal(Path.Combine(CustomRoot, "Yamori", "PC", "history"), location.Directory);
        Assert.Equal([location], changed);
        Assert.Equal([10, 20], service.Store.Read(HardwareComponent.Cpu, T0, T0.AddHours(1)).Select(s => s.UsagePercent));

        // 元の記録は消さない。
        Assert.Single(Directory.GetFiles(LocalDirectory));
    }

    [Fact]
    public void SwitchingBackMergesWithoutDuplicates()
    {
        Directory.CreateDirectory(CustomRoot);
        var custom = new YamoriSettings { StorageLocation = StorageLocationKind.CustomFolder, CustomFolderPath = CustomRoot };
        var service = Create(YamoriSettings.Default);
        service.Store.Append([Sample(0, 10)]);

        service.Apply(custom);
        service.Store.Append([Sample(1, 20)]);
        service.Apply(YamoriSettings.Default);

        Assert.Equal([10, 20], service.Store.Read(HardwareComponent.Cpu, T0, T0.AddHours(1)).Select(s => s.UsagePercent));
    }

    [Fact]
    public void ApplyingTheSameLocationDoesNotRaiseAnEvent()
    {
        var service = Create(YamoriSettings.Default);
        var raised = false;
        service.LocationChanged += (_, _) => raised = true;

        service.Apply(YamoriSettings.Default with { Language = AppLanguage.English });

        Assert.False(raised);
    }
}

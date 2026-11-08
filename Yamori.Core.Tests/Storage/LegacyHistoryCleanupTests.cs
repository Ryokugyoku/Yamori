using Yamori.Core.Storage.Legacy;

namespace Yamori.Core.Tests.Storage;

public sealed class LegacyHistoryCleanupTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "YamoriTests", Guid.NewGuid().ToString("N"));

    private string Local => Path.Combine(_root, "LocalAppData", "Yamori");

    private string SettingsFile => Path.Combine(Local, "settings.json");

    private string OneDrive => Path.Combine(_root, "OneDrive");

    private string Custom => Path.Combine(_root, "Custom");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private LegacyHistoryLocator CreateLocator(Dictionary<string, string>? environment = null) =>
        new(Local, SettingsFile, "MY:PC", name => environment?.GetValueOrDefault(name));

    private static string WriteFile(string directory, string name)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, "x");
        return path;
    }

    [Fact]
    public void FindsLocalOneDriveAndCustomFolders()
    {
        Directory.CreateDirectory(Local);
        File.WriteAllText(SettingsFile, $$"""{ "StorageLocation": "CustomFolder", "CustomFolderPath": {{System.Text.Json.JsonSerializer.Serialize(Custom)}} }""");

        var locations = CreateLocator(new() { ["OneDrive"] = OneDrive, ["OneDriveConsumer"] = OneDrive }).Find();

        Assert.Equal(
            [
                Path.Combine(Local, "history"),
                Path.Combine(OneDrive, "Yamori", "MY_PC", "history"),
                Path.Combine(Custom, "Yamori", "MY_PC", "history"),
            ],
            locations.Select(l => l.Directory));
        Assert.Empty(locations[0].ParentsToRemoveIfEmpty);
        Assert.Equal([Path.Combine(OneDrive, "Yamori", "MY_PC"), Path.Combine(OneDrive, "Yamori")], locations[1].ParentsToRemoveIfEmpty);
    }

    [Fact]
    public void IgnoresBrokenSettingsAndRelativePaths()
    {
        Directory.CreateDirectory(Local);
        File.WriteAllText(SettingsFile, "{ not json");

        var locations = CreateLocator(new() { ["OneDrive"] = "relative\\path" }).Find();

        Assert.Equal([Path.Combine(Local, "history")], locations.Select(l => l.Directory));
    }

    [Fact]
    public void DeletesOnlyLegacyCsvFilesAndEmptyFolders()
    {
        var localHistory = Path.Combine(Local, "history");
        WriteFile(localHistory, "usage-2026-11-06.csv");
        WriteFile(Local, "settings.json");

        var oneDriveHistory = Path.Combine(OneDrive, "Yamori", "MY_PC", "history");
        WriteFile(oneDriveHistory, "usage-2026-11-07.csv");

        var deleted = LegacyHistoryCleaner.Clean(CreateLocator(new() { ["OneDrive"] = OneDrive }).Find());

        Assert.Equal(2, deleted);
        Assert.False(Directory.Exists(localHistory));
        Assert.True(File.Exists(SettingsFile));
        Assert.False(Directory.Exists(Path.Combine(OneDrive, "Yamori")));
        Assert.True(Directory.Exists(OneDrive));
    }

    [Fact]
    public void KeepsFoldersThatHoldOtherFiles()
    {
        var history = Path.Combine(OneDrive, "Yamori", "MY_PC", "history");
        WriteFile(history, "usage-2026-11-07.csv");
        var other = WriteFile(history, "usage-notes.csv");
        var sibling = WriteFile(Path.Combine(OneDrive, "Yamori"), "readme.txt");

        var deleted = LegacyHistoryCleaner.Clean(CreateLocator(new() { ["OneDrive"] = OneDrive }).Find());

        Assert.Equal(1, deleted);
        Assert.True(File.Exists(other));
        Assert.True(File.Exists(sibling));
    }

    [Fact]
    public void RemovesEmptyParentsUpToTheFirstNonEmptyOne()
    {
        var history = Path.Combine(OneDrive, "Yamori", "MY_PC", "history");
        WriteFile(history, "usage-2026-11-07.csv");
        WriteFile(Path.Combine(OneDrive, "Yamori", "OTHER-PC", "history"), "usage-2026-11-07.csv");

        LegacyHistoryCleaner.Clean(CreateLocator(new() { ["OneDrive"] = OneDrive }).Find());

        Assert.False(Directory.Exists(Path.Combine(OneDrive, "Yamori", "MY_PC")));
        Assert.True(Directory.Exists(Path.Combine(OneDrive, "Yamori", "OTHER-PC", "history")));
    }
}

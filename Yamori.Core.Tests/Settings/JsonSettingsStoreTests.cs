using Yamori.Core.Settings;

namespace Yamori.Core.Tests.Settings;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "YamoriTests", Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void MissingFileGivesDefaults()
    {
        Assert.Equal(YamoriSettings.Default, new JsonSettingsStore(FilePath).Load());
    }

    [Fact]
    public void RoundTrips()
    {
        var store = new JsonSettingsStore(FilePath);
        var settings = new YamoriSettings { Language = AppLanguage.Spanish };

        store.Save(settings);

        Assert.Equal(settings, store.Load());
        Assert.Contains("\"Spanish\"", File.ReadAllText(FilePath));
    }

    [Fact]
    public void IgnoresStorageLocationFromOlderVersions()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, """{ "StorageLocation": "OneDrive", "CustomFolderPath": "D:\\Data", "Language": "Japanese" }""");

        Assert.Equal(new YamoriSettings { Language = AppLanguage.Japanese }, new JsonSettingsStore(FilePath).Load());
    }

    [Fact]
    public void BrokenFileGivesDefaults()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, "{ not json");

        Assert.Equal(YamoriSettings.Default, new JsonSettingsStore(FilePath).Load());
    }
}

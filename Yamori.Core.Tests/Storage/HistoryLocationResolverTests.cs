using Yamori.Core.Settings;
using Yamori.Core.Storage;

namespace Yamori.Core.Tests.Storage;

public class HistoryLocationResolverTests
{
    private const string Local = @"C:\Users\me\AppData\Local\Yamori\history";

    private static HistoryLocationResolver Create(string? oneDrive, params string[] existingFolders)
    {
        var locator = new OneDriveLocator(
            name => name == "OneDrive" ? oneDrive : null,
            path => path == oneDrive || existingFolders.Contains(path));
        return new HistoryLocationResolver(locator, Local, "MY-PC", existingFolders.Contains);
    }

    [Fact]
    public void LocalIsTheDefault()
    {
        Assert.Equal(new HistoryLocation(Local, StorageLocationKind.Local, false), Create(null).Resolve(YamoriSettings.Default));
    }

    [Fact]
    public void OneDriveUsesAFolderPerPc()
    {
        var location = Create(@"C:\Users\me\OneDrive").Resolve(new YamoriSettings { StorageLocation = StorageLocationKind.OneDrive });

        Assert.Equal(@"C:\Users\me\OneDrive\Yamori\MY-PC\history", location.Directory);
        Assert.Equal(StorageLocationKind.OneDrive, location.Kind);
        Assert.True(location.MayLeaveThisPc);
    }

    [Fact]
    public void MissingOneDriveFallsBackToLocal()
    {
        var location = Create(null).Resolve(new YamoriSettings { StorageLocation = StorageLocationKind.OneDrive });

        Assert.Equal(new HistoryLocation(Local, StorageLocationKind.Local, true), location);
    }

    [Fact]
    public void CustomFolderUsesAFolderPerPc()
    {
        var location = Create(null, @"D:\Data").Resolve(new YamoriSettings
        {
            StorageLocation = StorageLocationKind.CustomFolder,
            CustomFolderPath = @"D:\Data",
        });

        Assert.Equal(@"D:\Data\Yamori\MY-PC\history", location.Directory);
        Assert.False(location.IsFallback);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("relative\\path")]
    [InlineData(@"E:\Unplugged")]
    public void UnusableCustomFolderFallsBackToLocal(string? path)
    {
        var location = Create(null, @"D:\Data").Resolve(new YamoriSettings
        {
            StorageLocation = StorageLocationKind.CustomFolder,
            CustomFolderPath = path,
        });

        Assert.True(location.IsFallback);
        Assert.Equal(Local, location.Directory);
    }
}

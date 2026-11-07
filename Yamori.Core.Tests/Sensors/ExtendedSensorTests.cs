using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Extended.Signing;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Sensors;

public class ExtendedSensorTests
{
    [Theory]
    [InlineData(false, ExtendedSensorState.NotInstalled)]
    [InlineData(true, ExtendedSensorState.HelperRequired)]
    public void OutsideHelperNeverUsesPawnIo(bool installed, ExtendedSensorState expected)
    {
        var control = new OutsideHelperExtendedSensorControl(new FixedProbe(installed));

        Assert.Equal(expected, control.GetState(discoveryFailed: false));
        Assert.False(control.CanUsePawnIo);
        Assert.Equal(SensorSettingResult.Unavailable, control.SetConsent(true));
    }

    [Theory]
    [InlineData(false, true, true, false, ExtendedSensorState.NotInstalled)]
    [InlineData(true, false, true, false, ExtendedSensorState.UnsupportedEdition)]
    [InlineData(true, true, false, false, ExtendedSensorState.ConsentRequired)]
    [InlineData(true, true, true, true, ExtendedSensorState.Unavailable)]
    [InlineData(true, true, true, false, ExtendedSensorState.Active)]
    public void HelperStateFollowsInstallationEditionConsentAndDiscovery(
        bool installed, bool official, bool consented, bool failed, ExtendedSensorState expected) =>
        Assert.Equal(expected, ExtendedSensorStatusResolver.ResolveInHelper(installed, official, consented, failed));

    [Fact]
    public void ConsentIsSavedAndReloaded()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "nested", "pawnio-consent.json");
        var control = new HelperExtendedSensorControl(new FixedProbe(true), new FixedEdition(true), new PawnIoConsentStore(path));
        var changes = 0;
        control.ConsentChanged += (_, _) => changes++;

        Assert.Equal(ExtendedSensorState.ConsentRequired, control.GetState(false));
        Assert.False(control.CanUsePawnIo);

        Assert.Equal(SensorSettingResult.Applied, control.SetConsent(true));
        Assert.Equal(SensorSettingResult.Applied, control.SetConsent(true));

        Assert.Equal(1, changes);
        Assert.True(control.CanUsePawnIo);
        Assert.Equal(ExtendedSensorState.Active, control.GetState(false));
        Assert.True(new HelperExtendedSensorControl(new FixedProbe(true), new FixedEdition(true), new PawnIoConsentStore(path)).IsConsented);
    }

    [Fact]
    public void UnofficialEditionIsNeverUsedEvenWithConsent()
    {
        using var temp = new TempDirectory();
        var store = new PawnIoConsentStore(Path.Combine(temp.Path, "consent.json"));
        store.Save(true);

        var control = new HelperExtendedSensorControl(new FixedProbe(true), new FixedEdition(false), store);

        Assert.False(control.CanUsePawnIo);
        Assert.Equal(ExtendedSensorState.UnsupportedEdition, control.GetState(false));
    }

    [Fact]
    public void BrokenConsentFileMeansNoConsent()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "consent.json");
        File.WriteAllText(path, "{ not json");

        Assert.False(new PawnIoConsentStore(path).Load());
    }

    [Theory]
    [InlineData("CN=Microsoft Windows Hardware Compatibility Publisher, O=Microsoft Corporation, L=Redmond, S=Washington, C=US", true)]
    [InlineData("CN=Someone Else, O=Microsoft Windows Hardware Compatibility Publisher", false)]
    [InlineData(null, false)]
    public void OnlyMicrosoftDriverSignerIsOfficial(string? subject, bool expected) =>
        Assert.Equal(expected, PawnIoEditionVerifier.IsMicrosoftDriverSigner(subject));

    [Theory]
    [InlineData("PawnIO Official Build", true)]
    [InlineData("PawnIO Unrestricted Build", false)]
    [InlineData(null, false)]
    public void OnlyOfficialFileDescriptionIsOfficial(string? description, bool expected) =>
        Assert.Equal(expected, PawnIoEditionVerifier.IsOfficialDescription(description));

    [Fact]
    public void DriverImagePathIsNormalized()
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        Assert.Equal(Path.Combine(windows, @"System32\drivers\x.sys"), DriverImagePathResolver.Normalize(@"\SystemRoot\System32\drivers\x.sys"));
        Assert.Equal(Path.Combine(windows, @"System32\drivers\x.sys"), DriverImagePathResolver.Normalize(@"System32\drivers\x.sys"));
        Assert.Equal(@"C:\Drivers\x.sys", DriverImagePathResolver.Normalize(@"\??\C:\Drivers\x.sys"));
    }

    private sealed class FixedProbe(bool installed) : IPawnIoInstallationProbe
    {
        public bool IsInstalled() => installed;
    }

    private sealed class FixedEdition(bool official) : IPawnIoEditionVerifier
    {
        public bool IsOfficial() => official;
    }
}

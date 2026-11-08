using Yamori.Core.Notifications;

namespace Yamori.Core.Tests.Notifications;

public sealed class NotificationActivationArgumentsTests
{
    [Fact]
    public void AnomalyRoundTrips()
    {
        var text = NotificationActivationArguments.Format(NotificationActivationRequest.ForAnomaly(123));

        Assert.Equal("action=anomaly&id=123", text);
        Assert.Equal(NotificationActivationRequest.ForAnomaly(123), NotificationActivationArguments.Parse(text));
    }

    [Fact]
    public void OpenAppRoundTrips()
    {
        var text = NotificationActivationArguments.Format(NotificationActivationRequest.OpenApp);

        Assert.Equal(NotificationActivationRequest.OpenApp, NotificationActivationArguments.Parse(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("action=anomaly")]
    [InlineData("action=anomaly&id=-5")]
    [InlineData("action=anomaly&id=abc")]
    [InlineData("action=unknown&id=1")]
    public void UnreadableArgumentsOpenTheApp(string? text)
    {
        Assert.Equal(NotificationActivationRequest.OpenApp, NotificationActivationArguments.Parse(text));
    }

    [Fact]
    public void ParsingIgnoresOrderAndCase()
    {
        Assert.Equal(NotificationActivationRequest.ForAnomaly(7), NotificationActivationArguments.Parse("ID=7&Action=Anomaly"));
    }

    [Fact]
    public void AnomalyWithoutIdCannotBeFormatted()
    {
        Assert.Throws<ArgumentException>(() =>
            NotificationActivationArguments.Format(new NotificationActivationRequest(NotificationActivationKind.OpenAnomaly)));
    }
}

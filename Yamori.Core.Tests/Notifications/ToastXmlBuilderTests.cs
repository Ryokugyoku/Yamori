using System.Xml.Linq;
using Yamori.Core.Notifications;

namespace Yamori.Core.Tests.Notifications;

public sealed class ToastXmlBuilderTests
{
    [Fact]
    public void BuildsTitleBodyAndLaunchArguments()
    {
        var xml = XElement.Parse(ToastXmlBuilder.Build("見出し", "本文", NotificationActivationRequest.ForAnomaly(5)));

        Assert.Equal("action=anomaly&id=5", (string?)xml.Attribute("launch"));
        var texts = xml.Descendants("text").Select(t => t.Value).ToArray();
        Assert.Equal(["見出し", "本文"], texts);
        Assert.Equal("ToastGeneric", (string?)xml.Element("visual")?.Element("binding")?.Attribute("template"));
    }

    [Fact]
    public void EscapesMarkup()
    {
        var text = ToastXmlBuilder.Build("<b>&", "\"x\"", NotificationActivationRequest.OpenApp);

        var xml = XElement.Parse(text);
        Assert.Equal("<b>&", xml.Descendants("text").First().Value);
    }

    [Fact]
    public void EmptyTitleIsRejected()
    {
        Assert.Throws<ArgumentException>(() => ToastXmlBuilder.Build(" ", "本文", NotificationActivationRequest.OpenApp));
    }
}

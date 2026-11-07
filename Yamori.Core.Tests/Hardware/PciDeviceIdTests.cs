using Yamori.Core.Hardware.Graphics;

namespace Yamori.Core.Tests.Hardware;

public class PciDeviceIdTests
{
    [Theory]
    [InlineData(@"PCI\VEN_10DE&DEV_2203&SUBSYS_50911462&REV_A1\4&22F77229&0&0008", 0x10DEu, 0x2203u)]
    [InlineData(@"pci\ven_8086&dev_a780", 0x8086u, 0xA780u)]
    public void ParsesPnpDeviceIds(string pnp, uint vendor, uint device)
    {
        Assert.Equal(new PciDeviceId(vendor, device), PciDeviceId.TryParse(pnp));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(@"ROOT\DISPLAY\0000")]
    public void ReturnsNullForNonPciDevices(string? pnp)
    {
        Assert.Null(PciDeviceId.TryParse(pnp));
    }
}

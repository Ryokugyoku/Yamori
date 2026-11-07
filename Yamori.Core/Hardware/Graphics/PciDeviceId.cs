using System.Globalization;
using System.Text.RegularExpressions;

namespace Yamori.Core.Hardware.Graphics;

/// <summary>
/// PCI のベンダー ID とデバイス ID の組。
/// </summary>
/// <param name="VendorId">ベンダー ID（例：NVIDIA は 0x10DE）。</param>
/// <param name="DeviceId">デバイス ID。</param>
public readonly partial record struct PciDeviceId(uint VendorId, uint DeviceId)
{
    /// <summary>
    /// PnP デバイス ID（例：<c>PCI\VEN_10DE&amp;DEV_2203&amp;SUBSYS_...</c>）から読み取る。
    /// </summary>
    /// <param name="pnpDeviceId">PnP デバイス ID。</param>
    /// <returns>読み取れた場合は ID。PCI のデバイスでない場合などは null。</returns>
    public static PciDeviceId? TryParse(string? pnpDeviceId)
    {
        if (string.IsNullOrEmpty(pnpDeviceId))
        {
            return null;
        }

        var match = PnpPattern().Match(pnpDeviceId);
        if (!match.Success)
        {
            return null;
        }

        return new PciDeviceId(
            uint.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            uint.Parse(match.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    [GeneratedRegex(@"VEN_([0-9A-F]{4})&DEV_([0-9A-F]{4})", RegexOptions.IgnoreCase)]
    private static partial Regex PnpPattern();
}

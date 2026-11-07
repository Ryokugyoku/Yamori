using System.Globalization;
using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// Win32_BaseBoard・Win32_BIOS・Win32_ComputerSystem・Win32_SystemEnclosure から、マザーボードとファームウェアの情報を読み取る。
/// </summary>
/// <remarks>
/// どれか 1 つのクラスが読めなくても、読めた分だけで情報を作る。メーカーが値を設定していない項目は null にする。
/// </remarks>
public sealed class MotherboardInfoReader : IHardwareReader<MotherboardInfo?>
{
    private const string BaseBoardQuery = "SELECT Manufacturer, Product, Version FROM Win32_BaseBoard";
    private const string BiosQuery = "SELECT Manufacturer, SMBIOSBIOSVersion, ReleaseDate, SMBIOSMajorVersion, SMBIOSMinorVersion FROM Win32_BIOS";
    private const string SystemQuery = "SELECT Manufacturer, Model FROM Win32_ComputerSystem";
    private const string EnclosureQuery = "SELECT ChassisTypes FROM Win32_SystemEnclosure";

    private readonly IWmiQueryExecutor _wmi;

    /// <summary>
    /// 読み取りに使う WMI 実行器を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public MotherboardInfoReader(IWmiQueryExecutor wmi)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        _wmi = wmi;
    }

    /// <inheritdoc />
    /// <returns>マザーボードの情報。どのクラスからも何も読めない場合は null。</returns>
    public MotherboardInfo? Read()
    {
        var board = FirstOrNull(BaseBoardQuery);
        var bios = FirstOrNull(BiosQuery);
        var system = FirstOrNull(SystemQuery);
        var enclosure = FirstOrNull(EnclosureQuery);
        if (board is null && bios is null && system is null && enclosure is null)
        {
            return null;
        }

        return new MotherboardInfo(
            Manufacturer: OemPlaceholderFilter.Clean(board?.GetString("Manufacturer")),
            Product: OemPlaceholderFilter.Clean(board?.GetString("Product")))
        {
            Version = OemPlaceholderFilter.Clean(board?.GetString("Version")),
            SystemManufacturer = OemPlaceholderFilter.Clean(system?.GetString("Manufacturer")),
            SystemModel = OemPlaceholderFilter.Clean(system?.GetString("Model")),
            Chassis = enclosure is null ? ChassisKind.Unknown : ChassisKindMapper.Map(enclosure.GetInt32Array("ChassisTypes")),
            BiosVendor = OemPlaceholderFilter.Clean(bios?.GetString("Manufacturer")),
            BiosVersion = OemPlaceholderFilter.Clean(bios?.GetString("SMBIOSBIOSVersion")),
            BiosReleaseDate = bios?.GetDate("ReleaseDate"),
            SmbiosVersion = bios?.GetInt32("SMBIOSMajorVersion") is { } major and > 0
                ? string.Create(CultureInfo.InvariantCulture, $"{major}.{bios.GetInt32("SMBIOSMinorVersion") ?? 0}")
                : null,
        };
    }

    private WmiRecord? FirstOrNull(string query) =>
        _wmi.QueryOrEmpty(WmiNamespaces.Cimv2, query) is { Count: > 0 } rows ? rows[0] : null;
}

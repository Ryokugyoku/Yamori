namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// Win32_PhysicalMemory の番号（SMBIOSMemoryType、FormFactor）を、規格や形状の名前にする。
/// </summary>
public static class MemoryTypeNames
{
    private static readonly IReadOnlyDictionary<int, string> SmbiosTypes = new Dictionary<int, string>
    {
        [18] = "DDR",
        [19] = "DDR2",
        [20] = "DDR",
        [21] = "DDR2",
        [24] = "DDR3",
        [26] = "DDR4",
        [27] = "LPDDR",
        [28] = "LPDDR2",
        [29] = "LPDDR3",
        [30] = "LPDDR4",
        [34] = "DDR5",
        [35] = "LPDDR5",
    };

    private static readonly IReadOnlyDictionary<int, string> FormFactors = new Dictionary<int, string>
    {
        [8] = "DIMM",
        [12] = "SODIMM",
    };

    /// <summary>
    /// SMBIOS のメモリの種類の番号から、規格の名前を取得する。
    /// </summary>
    /// <param name="smbiosMemoryType">SMBIOSMemoryType の値。</param>
    /// <returns>規格の名前（例：DDR5）。分からない場合は null。</returns>
    public static string? GetTypeName(int? smbiosMemoryType) =>
        smbiosMemoryType is { } value && SmbiosTypes.TryGetValue(value, out var name) ? name : null;

    /// <summary>
    /// FormFactor の番号から、形状の名前を取得する。
    /// </summary>
    /// <param name="formFactor">FormFactor の値。</param>
    /// <returns>形状の名前（例：DIMM）。分からない場合は null。</returns>
    public static string? GetFormFactorName(int? formFactor) =>
        formFactor is { } value && FormFactors.TryGetValue(value, out var name) ? name : null;
}

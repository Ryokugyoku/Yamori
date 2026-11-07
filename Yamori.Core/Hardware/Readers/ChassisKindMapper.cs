namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// SMBIOS の筐体の種類の番号（Win32_SystemEnclosure.ChassisTypes）を <see cref="ChassisKind"/> にまとめる。
/// </summary>
public static class ChassisKindMapper
{
    private static readonly IReadOnlyDictionary<int, ChassisKind> Kinds = new Dictionary<int, ChassisKind>
    {
        [1] = ChassisKind.Unknown,
        [2] = ChassisKind.Unknown,
        [3] = ChassisKind.Desktop,
        [4] = ChassisKind.Desktop,
        [5] = ChassisKind.Desktop,
        [6] = ChassisKind.Desktop,
        [7] = ChassisKind.Desktop,
        [8] = ChassisKind.Laptop,
        [9] = ChassisKind.Laptop,
        [10] = ChassisKind.Laptop,
        [11] = ChassisKind.Tablet,
        [13] = ChassisKind.AllInOne,
        [14] = ChassisKind.Laptop,
        [15] = ChassisKind.Desktop,
        [16] = ChassisKind.Desktop,
        [17] = ChassisKind.Server,
        [23] = ChassisKind.Server,
        [24] = ChassisKind.Desktop,
        [25] = ChassisKind.Server,
        [28] = ChassisKind.Server,
        [29] = ChassisKind.Server,
        [30] = ChassisKind.Tablet,
        [31] = ChassisKind.Convertible,
        [32] = ChassisKind.Convertible,
        [35] = ChassisKind.MiniPc,
        [36] = ChassisKind.MiniPc,
    };

    /// <summary>
    /// 筐体の種類の番号の一覧から、最初に分かったものを返す。
    /// </summary>
    /// <param name="chassisTypes">筐体の種類の番号の一覧。</param>
    /// <returns>筐体の形。どれも分からない場合は <see cref="ChassisKind.Unknown"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="chassisTypes"/> が null。</exception>
    public static ChassisKind Map(IEnumerable<int> chassisTypes)
    {
        ArgumentNullException.ThrowIfNull(chassisTypes);
        foreach (var type in chassisTypes)
        {
            var kind = Kinds.TryGetValue(type, out var known) ? known : ChassisKind.Other;
            if (kind != ChassisKind.Unknown)
            {
                return kind;
            }
        }

        return ChassisKind.Unknown;
    }
}

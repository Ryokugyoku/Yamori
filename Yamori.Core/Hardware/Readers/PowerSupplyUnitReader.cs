using Yamori.Core.Hardware.Smbios;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// SMBIOS の System Power Supply（Type 39）から、ファームウェアが報告している電源ユニットの一覧を読み取る。
/// </summary>
/// <remarks>
/// 自作 PC 用の電源ユニットは、マザーボードに型番や容量を伝えないため、多くの PC では値が空か決まり文句になる。
/// </remarks>
public sealed class PowerSupplyUnitReader : IHardwareReader<IReadOnlyList<PowerSupplyUnitInfo>>
{
    private const int LocationOffset = 0x05;
    private const int DeviceNameOffset = 0x06;
    private const int ManufacturerOffset = 0x07;
    private const int ModelPartNumberOffset = 0x0A;
    private const int MaxPowerCapacityOffset = 0x0C;
    private const int CharacteristicsOffset = 0x0E;

    /// <summary>最大出力が「分からない」を表す値。</summary>
    private const ushort UnknownCapacity = 0x8000;

    private const int HotReplaceableBit = 1 << 0;
    private const int PresentBit = 1 << 1;

    private readonly ISmbiosTableSource _smbios;

    /// <summary>
    /// 読み取りに使う SMBIOS の情報源を指定して作る。
    /// </summary>
    /// <param name="smbios">SMBIOS テーブルの情報源。</param>
    /// <exception cref="ArgumentNullException"><paramref name="smbios"/> が null。</exception>
    public PowerSupplyUnitReader(ISmbiosTableSource smbios)
    {
        ArgumentNullException.ThrowIfNull(smbios);
        _smbios = smbios;
    }

    /// <inheritdoc />
    /// <returns>取り付けられている電源ユニットの一覧。ファームウェアが報告していない場合は空。</returns>
    public IReadOnlyList<PowerSupplyUnitInfo> Read() =>
        _smbios.Read()?.OfType(SmbiosStructureTypes.SystemPowerSupply)
            .Where(IsPresent)
            .Select(ToUnit)
            .ToArray() ?? [];

    private static bool IsPresent(SmbiosStructure structure) =>
        structure.GetWord(CharacteristicsOffset) is not { } characteristics || (characteristics & PresentBit) != 0;

    private static PowerSupplyUnitInfo ToUnit(SmbiosStructure structure)
    {
        var characteristics = structure.GetWord(CharacteristicsOffset) ?? 0;
        var capacity = structure.GetWord(MaxPowerCapacityOffset);
        return new PowerSupplyUnitInfo(
            Name: OemPlaceholderFilter.Clean(structure.GetString(DeviceNameOffset)) ?? OemPlaceholderFilter.Clean(structure.GetString(LocationOffset)),
            Manufacturer: OemPlaceholderFilter.Clean(structure.GetString(ManufacturerOffset)),
            Model: OemPlaceholderFilter.Clean(structure.GetString(ModelPartNumberOffset)),
            MaxPowerWatts: capacity is { } watts and > 0 and not UnknownCapacity ? watts : null,
            Kind: ToKind((characteristics >> 10) & 0x0F),
            Status: SmbiosStatus.FromValue((characteristics >> 7) & 0x07),
            IsHotReplaceable: (characteristics & HotReplaceableBit) != 0);
    }

    private static PowerSupplyKind ToKind(int value) =>
        Enum.IsDefined(typeof(PowerSupplyKind), value) ? (PowerSupplyKind)value : PowerSupplyKind.Unknown;
}

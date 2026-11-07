using Yamori.Core.Hardware.Smbios;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// SMBIOS の Cooling Device（Type 27）から、ファームウェアが報告している冷却装置の一覧を読み取る。
/// </summary>
/// <remarks>
/// Win32_Fan も同じ情報を元にしているが、種類と公称回転数は SMBIOS を直接読まないと分からない。
/// 回転数の実測値はここにも含まれない。
/// </remarks>
public sealed class FanInfoReader : IHardwareReader<IReadOnlyList<FanInfo>>
{
    private const int DeviceTypeAndStatusOffset = 0x06;
    private const int NominalSpeedOffset = 0x0C;
    private const int DescriptionOffset = 0x0E;

    /// <summary>公称回転数が「分からない」を表す値。</summary>
    private const ushort UnknownSpeed = 0x8000;

    private readonly ISmbiosTableSource _smbios;

    /// <summary>
    /// 読み取りに使う SMBIOS の情報源を指定して作る。
    /// </summary>
    /// <param name="smbios">SMBIOS テーブルの情報源。</param>
    /// <exception cref="ArgumentNullException"><paramref name="smbios"/> が null。</exception>
    public FanInfoReader(ISmbiosTableSource smbios)
    {
        ArgumentNullException.ThrowIfNull(smbios);
        _smbios = smbios;
    }

    /// <inheritdoc />
    /// <returns>冷却装置の一覧。ファームウェアが報告していない場合は空。</returns>
    public IReadOnlyList<FanInfo> Read() =>
        _smbios.Read()?.OfType(SmbiosStructureTypes.CoolingDevice).Select(ToFan).ToArray() ?? [];

    private static FanInfo ToFan(SmbiosStructure structure)
    {
        var typeAndStatus = structure.GetByte(DeviceTypeAndStatusOffset) ?? 0;
        var speed = structure.GetWord(NominalSpeedOffset);
        return new FanInfo(
            Description: OemPlaceholderFilter.Clean(structure.GetString(DescriptionOffset)),
            Type: ToType(typeAndStatus & 0x1F),
            Status: SmbiosStatus.FromValue(typeAndStatus >> 5),
            NominalSpeedRpm: speed is { } rpm and > 0 and not UnknownSpeed ? rpm : null);
    }

    private static CoolingDeviceType ToType(int value) =>
        Enum.IsDefined(typeof(CoolingDeviceType), value) ? (CoolingDeviceType)value : CoolingDeviceType.Unknown;
}

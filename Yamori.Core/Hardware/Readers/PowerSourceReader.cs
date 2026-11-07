using Yamori.Core.Interop;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// Windows の GetSystemPowerStatus API で、今使っている電源（AC 電源かバッテリーか）を読み取る。
/// </summary>
public sealed class PowerSourceReader : IHardwareReader<PowerSource>
{
    private const byte AcOffline = 0;
    private const byte AcOnline = 1;

    /// <inheritdoc />
    /// <returns>今使っている電源。分からない場合は <see cref="PowerSource.Unknown"/>。</returns>
    public PowerSource Read()
    {
        if (!NativeMethods.GetSystemPowerStatus(out var status))
        {
            return PowerSource.Unknown;
        }

        return status.AcLineStatus switch
        {
            AcOnline => PowerSource.Ac,
            AcOffline => PowerSource.Battery,
            _ => PowerSource.Unknown,
        };
    }
}

using Yamori.Core.Hardware;
using Yamori.Core.Interop;

namespace Yamori.Core.Realtime.Sources;

/// <summary>
/// Windows の GetSystemPowerStatus API で、今の電源の状態を読み取る。
/// </summary>
public sealed class Win32PowerStatusSource : IPowerStatusSource
{
    private const byte AcOffline = 0;
    private const byte AcOnline = 1;
    private const byte NoBatteryFlag = 128;
    private const byte UnknownBatteryFlag = 255;
    private const byte UnknownPercent = 255;
    private const uint UnknownLifeTime = uint.MaxValue;

    /// <inheritdoc />
    public PowerStatus? Read()
    {
        if (!NativeMethods.GetSystemPowerStatus(out var status))
        {
            return null;
        }

        var source = status.AcLineStatus switch
        {
            AcOnline => PowerSource.Ac,
            AcOffline => PowerSource.Battery,
            _ => PowerSource.Unknown,
        };

        var hasBattery = status.BatteryFlag != UnknownBatteryFlag && (status.BatteryFlag & NoBatteryFlag) == 0;
        return new PowerStatus(
            Source: source,
            HasBattery: hasBattery,
            BatteryPercent: hasBattery && status.BatteryLifePercent != UnknownPercent ? status.BatteryLifePercent : null,
            BatteryRemaining: hasBattery && status.BatteryLifeTime != UnknownLifeTime ? TimeSpan.FromSeconds(status.BatteryLifeTime) : null,
            IsBatterySaverOn: status.SystemStatusFlag == 1);
    }
}

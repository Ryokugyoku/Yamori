using Yamori.Core.Hardware;
using Yamori.Core.Sensors;

namespace Yamori.Presentation.Sensors;

/// <summary>
/// センサーの表示で使うアイコン（Segoe Fluent Icons の文字）。
/// </summary>
public static class SensorGlyphs
{
    /// <summary>バッテリーのアイコン。</summary>
    private const string Battery = "";

    /// <summary>
    /// ハードウェアの種類に対応するアイコンを取得する。
    /// </summary>
    /// <param name="kind">ハードウェアの種類。</param>
    /// <returns>アイコンの文字。</returns>
    public static string For(HardwareKind kind) =>
        kind switch
        {
            HardwareKind.Cpu => HardwareGlyphs.For(HardwareComponent.Cpu),
            HardwareKind.Memory => HardwareGlyphs.For(HardwareComponent.Memory),
            HardwareKind.Gpu => HardwareGlyphs.For(HardwareComponent.Gpu),
            HardwareKind.Storage or HardwareKind.Volume => HardwareGlyphs.For(HardwareComponent.Storage),
            HardwareKind.Network => HardwareGlyphs.For(HardwareComponent.Network),
            HardwareKind.Battery => Battery,
            HardwareKind.Motherboard or HardwareKind.ThermalZone => HardwareGlyphs.For(HardwareComponent.Motherboard),
            HardwareKind.PowerSupply => HardwareGlyphs.For(HardwareComponent.PowerSupply),
            _ => HardwareGlyphs.Unknown,
        };

    /// <summary>
    /// サマリーカードのアイコンを取得する。表示しているセンサーのハードウェアの種類で決める。
    /// </summary>
    /// <param name="key">カードが表示しているセンサーのキー。</param>
    /// <returns>アイコンの文字。</returns>
    public static string ForCard(SensorKey key) => For(key.HardwareKind == HardwareKind.ThermalZone ? HardwareKind.Cpu : key.HardwareKind);
}

using Yamori.Core.Hardware;
using Yamori.Core.Localization;

namespace Yamori.Core.Formatting;

/// <summary>
/// 部品の種類の表示名。
/// </summary>
public static class ComponentDisplayNames
{
    /// <summary>
    /// 部品の種類の表示名を、現在の UI カルチャの言語で取得する。
    /// </summary>
    /// <param name="component">部品の種類。</param>
    /// <returns>表示名。</returns>
    public static string Get(HardwareComponent component) =>
        component switch
        {
            HardwareComponent.Cpu => CoreText.Get("Component_Cpu"),
            HardwareComponent.Motherboard => CoreText.Get("Component_Motherboard"),
            HardwareComponent.Memory => CoreText.Get("Component_Memory"),
            HardwareComponent.Gpu => CoreText.Get("Component_Gpu"),
            HardwareComponent.Storage => CoreText.Get("Component_Storage"),
            HardwareComponent.Fan => CoreText.Get("Component_Fan"),
            HardwareComponent.PowerSupply => CoreText.Get("Component_PowerSupply"),
            HardwareComponent.Network => CoreText.Get("Component_Network"),
            _ => component.ToString(),
        };
}

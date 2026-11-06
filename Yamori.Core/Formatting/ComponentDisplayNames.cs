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
            HardwareComponent.Memory => CoreText.Get("Component_Memory"),
            HardwareComponent.Storage => CoreText.Get("Component_Storage"),
            HardwareComponent.Gpu => CoreText.Get("Component_Gpu"),
            HardwareComponent.Battery => CoreText.Get("Component_Battery"),
            _ => component.ToString(),
        };
}

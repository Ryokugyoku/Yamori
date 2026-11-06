using Yamori.Core.Hardware;

namespace Yamori.Core.Formatting;

/// <summary>
/// 部品の種類の表示名。
/// </summary>
public static class ComponentDisplayNames
{
    /// <summary>
    /// 部品の種類の表示名を取得する。
    /// </summary>
    /// <param name="component">部品の種類。</param>
    /// <returns>日本語の表示名。</returns>
    public static string Get(HardwareComponent component) =>
        component switch
        {
            HardwareComponent.Cpu => "CPU",
            HardwareComponent.Memory => "メモリ",
            HardwareComponent.Storage => "ストレージ",
            HardwareComponent.Gpu => "GPU",
            HardwareComponent.Battery => "バッテリー",
            _ => component.ToString(),
        };
}

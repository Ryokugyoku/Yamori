using Yamori.Core.Hardware;

namespace Yamori.Presentation;

/// <summary>
/// 部品の種類ごとのアイコン（Segoe Fluent Icons の文字）。
/// </summary>
public static class HardwareGlyphs
{
    /// <summary>
    /// 部品の種類に対応するアイコンを取得する。
    /// </summary>
    /// <param name="component">部品の種類。</param>
    /// <returns>アイコンの文字。</returns>
    public static string For(HardwareComponent component) =>
        component switch
        {
            HardwareComponent.Cpu => "",
            HardwareComponent.Memory => "",
            HardwareComponent.Storage => "",
            HardwareComponent.Gpu => "",
            HardwareComponent.Battery => "",
            _ => "",
        };

    /// <summary>
    /// 情報を取得できなかったときのアイコン。
    /// </summary>
    public const string Unknown = "";
}

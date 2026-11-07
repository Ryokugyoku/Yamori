using Yamori.Core.Hardware;

namespace Yamori.Presentation;

/// <summary>
/// 部品の種類ごとのアイコン（Segoe Fluent Icons の文字）。
/// </summary>
public static class HardwareGlyphs
{
    /// <summary>
    /// 情報を取得できなかったときのアイコン。
    /// </summary>
    public const string Unknown = "";

    /// <summary>
    /// アプリごとの使用量を表すアイコン。
    /// </summary>
    public const string Apps = "";

    /// <summary>
    /// 部品の種類に対応するアイコンを取得する。
    /// </summary>
    /// <param name="component">部品の種類。</param>
    /// <returns>アイコンの文字。</returns>
    public static string For(HardwareComponent component) =>
        component switch
        {
            HardwareComponent.Cpu => "",
            HardwareComponent.Motherboard => "",
            HardwareComponent.Memory => "",
            HardwareComponent.Gpu => "",
            HardwareComponent.Storage => "",
            HardwareComponent.Fan => "",
            HardwareComponent.PowerSupply => "",
            HardwareComponent.Network => "",
            _ => Unknown,
        };
}

using Yamori.Core.Sensors;

namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 分析画面の部品の種類と、センサーのハードウェアの種類の対応。
/// </summary>
public static class AnalysisParts
{
    /// <summary>左の一覧に常に出す部品（並べる順）。</summary>
    public static IReadOnlyList<AnalysisPart> Standard { get; } =
        [AnalysisPart.Cpu, AnalysisPart.Gpu, AnalysisPart.Memory, AnalysisPart.Storage, AnalysisPart.Network];

    /// <summary>異常があるときだけ、<see cref="Standard"/> のあとに出す部品。</summary>
    public static IReadOnlyList<AnalysisPart> Optional { get; } = [AnalysisPart.ThermalZone];

    /// <summary>
    /// センサーのハードウェアの種類から、分析画面の部品の種類を決める。
    /// </summary>
    /// <param name="kind">ハードウェアの種類。</param>
    /// <returns>部品の種類。分析画面で扱わない種類なら null。</returns>
    public static AnalysisPart? From(HardwareKind kind) => kind switch
    {
        HardwareKind.Cpu => AnalysisPart.Cpu,
        HardwareKind.Gpu => AnalysisPart.Gpu,
        HardwareKind.Memory => AnalysisPart.Memory,
        HardwareKind.Storage => AnalysisPart.Storage,
        HardwareKind.Network => AnalysisPart.Network,
        HardwareKind.ThermalZone => AnalysisPart.ThermalZone,
        _ => null,
    };

    /// <summary>
    /// 部品の種類に対応する、センサーのハードウェアの種類を取得する。
    /// </summary>
    /// <param name="part">部品の種類。</param>
    /// <returns>ハードウェアの種類。</returns>
    public static HardwareKind ToHardwareKind(AnalysisPart part) => part switch
    {
        AnalysisPart.Cpu => HardwareKind.Cpu,
        AnalysisPart.Gpu => HardwareKind.Gpu,
        AnalysisPart.Memory => HardwareKind.Memory,
        AnalysisPart.Storage => HardwareKind.Storage,
        AnalysisPart.Network => HardwareKind.Network,
        AnalysisPart.ThermalZone => HardwareKind.ThermalZone,
        _ => throw new ArgumentOutOfRangeException(nameof(part), part, "定義されていない部品の種類です。"),
    };
}

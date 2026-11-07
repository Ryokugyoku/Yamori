namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// コアの種類。
/// </summary>
public enum CpuCoreKind
{
    /// <summary>ハイブリッド構成ではない（すべてのコアが同じ種類）。</summary>
    Uniform = 0,

    /// <summary>P コア（性能を重視するコア）。</summary>
    Performance = 1,

    /// <summary>E コア（効率を重視するコア）。</summary>
    Efficiency = 2,
}

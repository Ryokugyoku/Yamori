namespace Yamori.Core.Hardware.Topology;

/// <summary>
/// ハイブリッド構成の CPU のコアの内訳。
/// </summary>
/// <param name="PerformanceCores">高性能コア（P コア）の数。</param>
/// <param name="EfficiencyCores">高効率コア（E コア）の数。</param>
public sealed record CoreTopology(int PerformanceCores, int EfficiencyCores);

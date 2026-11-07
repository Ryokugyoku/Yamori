namespace Yamori.Core.Hardware;

/// <summary>
/// CPU の情報。内蔵 GPU を持つ場合は <see cref="IntegratedGpu"/> も含む。
/// </summary>
/// <param name="Name">製品名（例：Intel(R) Core(TM) i7-1360P）。</param>
/// <param name="PhysicalCores">物理コア数。複数ソケットの場合は合計。</param>
/// <param name="LogicalProcessors">論理プロセッサ数（スレッド数）。複数ソケットの場合は合計。</param>
/// <param name="MaxClockMhz">最大クロック（MHz）。Windows が報告する定格値で、ブースト時の最大値ではないことがある。</param>
public sealed record CpuInfo(string Name, int PhysicalCores, int LogicalProcessors, int MaxClockMhz)
{
    /// <summary>メーカー（例：GenuineIntel、AuthenticAMD）。取得できない場合は null。</summary>
    public string? Manufacturer { get; init; }

    /// <summary>アーキテクチャ（例：x64、ARM64）。取得できない場合は null。</summary>
    public string? Architecture { get; init; }

    /// <summary>ソケットの名前（例：LGA1700、AM5）。取得できない場合は null。</summary>
    public string? Socket { get; init; }

    /// <summary>CPU（ソケット）の数。</summary>
    public int SocketCount { get; init; } = 1;

    /// <summary>高性能コア（P コア）の数。ハイブリッド構成でない場合は null。</summary>
    public int? PerformanceCores { get; init; }

    /// <summary>高効率コア（E コア）の数。ハイブリッド構成でない場合は null。</summary>
    public int? EfficiencyCores { get; init; }

    /// <summary>L2 キャッシュの合計（バイト）。取得できない場合は null。</summary>
    public ulong? L2CacheBytes { get; init; }

    /// <summary>L3 キャッシュの合計（バイト）。取得できない場合は null。</summary>
    public ulong? L3CacheBytes { get; init; }

    /// <summary>CPU に内蔵された GPU。内蔵 GPU がないか、判別できない場合は null。</summary>
    public GpuInfo? IntegratedGpu { get; init; }
}

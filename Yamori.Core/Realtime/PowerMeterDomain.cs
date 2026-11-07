namespace Yamori.Core.Realtime;

/// <summary>
/// 電力量計が測っている範囲。
/// </summary>
public enum PowerMeterDomain
{
    /// <summary>分からない、またはその他。</summary>
    Other,

    /// <summary>CPU のパッケージ全体（コア、キャッシュ、内蔵 GPU などを含む）。RAPL の pkg。</summary>
    CpuPackage,

    /// <summary>CPU のコア。RAPL の pp0。</summary>
    CpuCores,

    /// <summary>CPU の内蔵 GPU。RAPL の pp1。</summary>
    CpuGraphics,

    /// <summary>メモリ（DRAM）。RAPL の dram。</summary>
    Memory,

    /// <summary>プラットフォーム全体（SoC と周辺）。RAPL の psys。</summary>
    Platform,
}

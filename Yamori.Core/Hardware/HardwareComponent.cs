namespace Yamori.Core.Hardware;

/// <summary>
/// Yamori が扱うハードウェア部品の種類。
/// </summary>
public enum HardwareComponent
{
    /// <summary>CPU（プロセッサ）。</summary>
    Cpu,

    /// <summary>メモリ（RAM）。</summary>
    Memory,

    /// <summary>ストレージ（SSD／HDD）。</summary>
    Storage,

    /// <summary>GPU（グラフィックス）。</summary>
    Gpu,

    /// <summary>バッテリー。</summary>
    Battery,
}

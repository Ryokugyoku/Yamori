namespace Yamori.Core.Analysis.Parts;

/// <summary>
/// 分析画面の左の一覧に並べる部品の種類。
/// </summary>
public enum AnalysisPart
{
    /// <summary>CPU。</summary>
    Cpu,

    /// <summary>GPU。</summary>
    Gpu,

    /// <summary>メモリ。</summary>
    Memory,

    /// <summary>ストレージ（物理ディスク）。</summary>
    Storage,

    /// <summary>ネットワーク。</summary>
    Network,

    /// <summary>ACPI の温度センサー（温度ゾーン）。異常があるときだけ一覧に出す。</summary>
    ThermalZone,
}

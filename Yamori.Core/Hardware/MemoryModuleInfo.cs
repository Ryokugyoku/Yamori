namespace Yamori.Core.Hardware;

/// <summary>
/// メモリモジュール 1 枚の情報。
/// </summary>
/// <param name="Slot">差さっているスロットの名前（例：DIMM1、Controller0-DIMM1）。</param>
/// <param name="CapacityBytes">容量（バイト）。</param>
public sealed record MemoryModuleInfo(string Slot, ulong CapacityBytes)
{
    /// <summary>モジュールが対応する速度（MT/s）。取得できない場合は null。</summary>
    public int? RatedSpeedMtps { get; init; }

    /// <summary>実際に動いている速度（MT/s）。取得できない場合は null。</summary>
    public int? ConfiguredSpeedMtps { get; init; }

    /// <summary>規格（例：DDR4、DDR5、LPDDR5）。分からない場合は null。</summary>
    public string? TypeName { get; init; }

    /// <summary>形状（例：DIMM、SODIMM）。分からない場合は null。</summary>
    public string? FormFactor { get; init; }

    /// <summary>メーカー。</summary>
    public string? Manufacturer { get; init; }

    /// <summary>型番（パーツ番号）。</summary>
    public string? PartNumber { get; init; }

    /// <summary>動作電圧（mV）。取得できない場合は null。</summary>
    public int? ConfiguredVoltageMillivolts { get; init; }
}

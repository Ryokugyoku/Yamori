namespace Yamori.Core.Hardware;

/// <summary>
/// 搭載メモリの情報。
/// </summary>
/// <param name="TotalBytes">搭載メモリの合計容量（バイト）。</param>
/// <param name="ModuleCount">メモリモジュールの枚数。</param>
/// <param name="SpeedMhz">動作速度（MT/s）。モジュールごとに違う場合は最も遅い値。取得できない場合は null。</param>
public sealed record MemoryInfo(ulong TotalBytes, int ModuleCount, int? SpeedMhz)
{
    /// <summary>モジュールごとの情報。</summary>
    public IReadOnlyList<MemoryModuleInfo> Modules { get; init; } = [];

    /// <summary>マザーボードのメモリスロットの数。取得できない場合は null。</summary>
    public int? SlotCount { get; init; }

    /// <summary>マザーボードが対応する最大容量（バイト）。取得できない場合は null。</summary>
    public ulong? MaxCapacityBytes { get; init; }

    /// <summary>
    /// メモリの規格（例：DDR5）を取得する。モジュールから最初に分かったものを使う。
    /// </summary>
    /// <returns>規格が分からない場合は null。</returns>
    public string? TypeName => Modules.Select(m => m.TypeName).FirstOrDefault(t => t is not null);

    /// <summary>
    /// 空いているメモリスロットの数を取得する。増設できるかの目安になる。
    /// </summary>
    /// <returns>スロットの数が分からない場合は null。</returns>
    public int? FreeSlots => SlotCount is { } slots ? Math.Max(0, slots - ModuleCount) : null;
}

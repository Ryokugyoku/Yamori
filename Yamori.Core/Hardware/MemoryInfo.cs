namespace Yamori.Core.Hardware;

/// <summary>
/// 搭載メモリの情報。
/// </summary>
/// <param name="TotalBytes">搭載メモリの合計容量（バイト）。</param>
/// <param name="ModuleCount">メモリモジュールの枚数。</param>
/// <param name="SpeedMhz">動作速度（MT/s）。取得できない場合は null。</param>
public sealed record MemoryInfo(ulong TotalBytes, int ModuleCount, int? SpeedMhz);

namespace Yamori.Core.Hardware;

/// <summary>
/// CPU の基本情報。
/// </summary>
/// <param name="Name">製品名（例：Intel(R) Core(TM) i7-1360P）。</param>
/// <param name="PhysicalCores">物理コア数。</param>
/// <param name="LogicalProcessors">論理プロセッサ数（スレッド数）。</param>
/// <param name="MaxClockMhz">最大クロック（MHz）。</param>
public sealed record CpuInfo(string Name, int PhysicalCores, int LogicalProcessors, int MaxClockMhz);

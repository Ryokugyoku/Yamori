namespace Yamori.Core.Realtime;

/// <summary>
/// 論理プロセッサ（スレッド）1 つの今の状態。
/// </summary>
/// <param name="Index">論理プロセッサの通し番号（0 から）。</param>
/// <param name="UsagePercent">使用率（%）。</param>
/// <param name="ClockMhz">今の動作クロック（MHz）。分からない場合は null。</param>
public sealed record LogicalProcessorUsage(int Index, double UsagePercent, double? ClockMhz);

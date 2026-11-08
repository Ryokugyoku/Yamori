namespace Yamori.Core.AppUsage.Storage;

/// <summary>
/// 期間の中の、1 つのアプリの 1 分ごとの CPU 使用率の合計。
/// </summary>
/// <param name="Name">アプリのプロセス名。</param>
/// <param name="CpuPercentSum">記録したすべての 1 分の、平均の使用率（%）の合計。</param>
/// <param name="HighLoadCpuPercentSum">PC 全体の使用率がしきい値を超えていた 1 分だけの、平均の使用率（%）の合計。</param>
public sealed record AppCpuTotal(string Name, double CpuPercentSum, double HighLoadCpuPercentSum);

namespace Yamori.Core.AppUsage.Analysis;

/// <summary>
/// 期間の中で、1 つのアプリが CPU をどれだけ使っていたか。
/// </summary>
/// <param name="Name">アプリのプロセス名。</param>
/// <param name="AveragePercent">記録していたすべての時間での、平均の CPU 使用率（PC 全体を 100 とする %）。</param>
/// <param name="HighLoadAveragePercent">PC 全体の CPU 使用率がしきい値を超えていた時間だけでの、平均の CPU 使用率（%）。その時間がなければ 0。</param>
public sealed record AppCpuUsage(string Name, double AveragePercent, double HighLoadAveragePercent);

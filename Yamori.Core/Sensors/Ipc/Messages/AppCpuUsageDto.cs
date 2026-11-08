namespace Yamori.Core.Sensors.Ipc.Messages;

/// <summary>
/// パイプで送る <see cref="AppUsage.Analysis.AppCpuUsage"/>。項目は同じ。
/// </summary>
/// <param name="Name">アプリのプロセス名。</param>
/// <param name="AveragePercent">記録していたすべての時間での平均の CPU 使用率（%）。</param>
/// <param name="HighLoadAveragePercent">高負荷の時間だけでの平均の CPU 使用率（%）。</param>
public sealed record AppCpuUsageDto(string Name, double AveragePercent, double HighLoadAveragePercent);

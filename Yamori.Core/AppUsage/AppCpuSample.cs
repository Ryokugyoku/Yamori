namespace Yamori.Core.AppUsage;

/// <summary>
/// ある時点に測った、PC 全体とアプリごとの CPU 使用率。値は前回の測定からの平均。
/// </summary>
/// <param name="TimestampUtc">測った時刻（UTC）。</param>
/// <param name="TotalPercent">PC 全体の CPU 使用率（%。負荷時間の分析と同じ <c>% Processor Utility</c>。100 を上限とする）。</param>
/// <param name="Apps">CPU を使っていたアプリ（使用率が 0 のアプリは含めない）。</param>
public sealed record AppCpuSample(DateTimeOffset TimestampUtc, double TotalPercent, IReadOnlyList<AppCpuShare> Apps);

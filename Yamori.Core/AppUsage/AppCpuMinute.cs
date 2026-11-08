namespace Yamori.Core.AppUsage;

/// <summary>
/// 1 分間の、PC 全体とアプリごとの CPU 使用率の平均。記録の単位。
/// </summary>
/// <param name="MinuteStartUtc">その 1 分の始まり（UTC、秒以下は 0）。</param>
/// <param name="TotalPercent">PC 全体の CPU 使用率の平均（%）。</param>
/// <param name="SampleCount">平均に使った測定の回数。</param>
/// <param name="Apps">CPU を多く使っていたアプリの平均（使用率の大きい順。上位だけ）。</param>
public sealed record AppCpuMinute(DateTimeOffset MinuteStartUtc, double TotalPercent, int SampleCount, IReadOnlyList<AppCpuShare> Apps);

using Yamori.Core.Analysis;
using Yamori.Core.Hardware;

namespace Yamori.Core.Monitoring;

/// <summary>
/// どの部品の値かを含めた、使用率のサンプル。履歴として保存する単位。
/// </summary>
/// <param name="Component">部品の種類。</param>
/// <param name="Timestamp">測った時刻。</param>
/// <param name="UsagePercent">使用率（0〜100）。</param>
public readonly record struct ComponentUsageSample(
    HardwareComponent Component,
    DateTimeOffset Timestamp,
    double UsagePercent)
{
    /// <summary>
    /// 分析に使う <see cref="UsageSample"/> に変換する。
    /// </summary>
    /// <returns>部品の種類を除いたサンプル。</returns>
    public UsageSample ToUsageSample() => new(Timestamp, UsagePercent);
}

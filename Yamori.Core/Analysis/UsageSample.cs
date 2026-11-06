namespace Yamori.Core.Analysis;

/// <summary>
/// 1つの部品の使用率を、ある時刻に測った値。
/// </summary>
/// <param name="Timestamp">測った時刻。</param>
/// <param name="UsagePercent">使用率（0〜100）。</param>
public readonly record struct UsageSample(DateTimeOffset Timestamp, double UsagePercent);

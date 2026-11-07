using Yamori.Core.Realtime.Counters;
using Yamori.Core.Realtime.Sources;

namespace Yamori.Core.Realtime.Collectors;

/// <summary>
/// Windows のメモリの統計とパフォーマンスカウンターから、メモリ（RAM）の今の状態を集める。
/// </summary>
public sealed class MemoryRealtimeCollector
{
    private readonly IPerformanceCounterSampler _sampler;

    /// <summary>
    /// カウンターの読み出し元を指定して作る。
    /// </summary>
    /// <param name="sampler">カウンターの読み出し元。</param>
    /// <exception cref="ArgumentNullException"><paramref name="sampler"/> が null。</exception>
    public MemoryRealtimeCollector(IPerformanceCounterSampler sampler)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        _sampler = sampler;
    }

    /// <summary>
    /// メモリの今の状態を集める。
    /// </summary>
    /// <param name="system">Windows 全体のメモリの使い方。読めなかった場合は null。</param>
    /// <param name="meters">電力量計の今の値。</param>
    /// <returns>メモリの今の状態。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="meters"/> が null。</exception>
    public MemoryRealtime Collect(SystemPerformance? system, IReadOnlyList<PowerMeterReading> meters)
    {
        ArgumentNullException.ThrowIfNull(meters);

        var standbyParts = new[]
        {
            _sampler.ReadSingleUInt64(CounterPaths.StandbyCacheCoreBytes),
            _sampler.ReadSingleUInt64(CounterPaths.StandbyCacheNormalPriorityBytes),
            _sampler.ReadSingleUInt64(CounterPaths.StandbyCacheReserveBytes),
        };
        var dram = meters.Where(m => m.Domain == PowerMeterDomain.Memory).ToArray();

        return new MemoryRealtime
        {
            TotalBytes = system?.TotalPhysicalBytes,
            AvailableBytes = system?.AvailablePhysicalBytes,
            UsedBytes = system is { } s ? s.TotalPhysicalBytes - Math.Min(s.AvailablePhysicalBytes, s.TotalPhysicalBytes) : null,
            UsagePercent = system?.MemoryLoadPercent,
            CommittedBytes = system?.CommittedBytes,
            CommitLimitBytes = system?.CommitLimitBytes,
            CommitPeakBytes = system?.CommitPeakBytes,
            SystemCacheBytes = system?.SystemCacheBytes,
            PagedPoolBytes = system?.KernelPagedBytes,
            NonPagedPoolBytes = system?.KernelNonpagedBytes,
            StandbyBytes = standbyParts.Any(p => p is not null) ? standbyParts.Aggregate(0UL, (sum, p) => sum + (p ?? 0)) : null,
            ModifiedBytes = _sampler.ReadSingleUInt64(CounterPaths.ModifiedPageListBytes),
            FreeBytes = _sampler.ReadSingleUInt64(CounterPaths.FreeAndZeroPageListBytes),
            PageFaultsPerSecond = _sampler.ReadSingle(CounterPaths.PageFaultsPerSecond),
            PagesPerSecond = _sampler.ReadSingle(CounterPaths.PagesPerSecond),
            PagingFileUsagePercent = _sampler.ReadSingle(CounterPaths.PagingFileUsageTotal),
            PowerWatts = dram.Length > 0 ? dram.Sum(m => m.Watts) : null,
        };
    }
}

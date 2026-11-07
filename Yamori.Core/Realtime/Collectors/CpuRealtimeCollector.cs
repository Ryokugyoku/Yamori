using System.Globalization;
using Yamori.Core.Realtime.Counters;
using Yamori.Core.Realtime.Sources;

namespace Yamori.Core.Realtime.Collectors;

/// <summary>
/// パフォーマンスカウンターなどから、CPU の今の状態を集める。
/// </summary>
public sealed class CpuRealtimeCollector
{
    private readonly IPerformanceCounterSampler _sampler;

    /// <summary>
    /// カウンターの読み出し元を指定して作る。
    /// </summary>
    /// <param name="sampler">カウンターの読み出し元。</param>
    /// <exception cref="ArgumentNullException"><paramref name="sampler"/> が null。</exception>
    public CpuRealtimeCollector(IPerformanceCounterSampler sampler)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        _sampler = sampler;
    }

    /// <summary>
    /// CPU の今の状態を集める。
    /// </summary>
    /// <param name="system">Windows 全体のプロセス・スレッド・ハンドルの数。読めなかった場合は null。</param>
    /// <param name="meters">電力量計の今の値。</param>
    /// <returns>CPU の今の状態。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="meters"/> が null。</exception>
    public CpuRealtime Collect(SystemPerformance? system, IReadOnlyList<PowerMeterReading> meters)
    {
        ArgumentNullException.ThrowIfNull(meters);

        var time = _sampler.Read(CounterPaths.ProcessorTime);
        var utility = _sampler.Read(CounterPaths.ProcessorUtility);
        var performance = _sampler.Read(CounterPaths.ProcessorPerformance);
        var frequency = _sampler.Read(CounterPaths.ProcessorFrequency);

        // タスク マネージャーと同じ % Processor Utility を主な値にする。古い Windows で読めない場合は % Processor Time を使う。
        var usage = utility.Count > 0 ? utility : time;
        var totalPerformance = Lookup(performance, CounterPaths.TotalInstance);
        var baseClock = Lookup(frequency, CounterPaths.TotalInstance);
        var totalTime = Clamp(Lookup(time, CounterPaths.TotalInstance));

        return new CpuRealtime
        {
            UsagePercent = Clamp(Lookup(usage, CounterPaths.TotalInstance)) ?? totalTime,
            ProcessorTimePercent = totalTime,
            UserPercent = Clamp(_sampler.ReadSingle(CounterPaths.ProcessorUserTimeTotal)),
            PrivilegedPercent = Clamp(_sampler.ReadSingle(CounterPaths.ProcessorPrivilegedTimeTotal)),
            InterruptPercent = Clamp(_sampler.ReadSingle(CounterPaths.ProcessorInterruptTimeTotal)),
            DpcPercent = Clamp(_sampler.ReadSingle(CounterPaths.ProcessorDpcTimeTotal)),
            ClockMhz = baseClock * totalPerformance / 100,
            BaseClockMhz = baseClock is > 0 ? baseClock : null,
            PerformancePercent = totalPerformance,
            PerformanceLimitPercent = _sampler.ReadSingle(CounterPaths.ProcessorPerformanceLimitTotal),
            QueueLength = _sampler.ReadSingle(CounterPaths.ProcessorQueueLength),
            ContextSwitchesPerSecond = _sampler.ReadSingle(CounterPaths.ContextSwitchesPerSecond),
            SystemCallsPerSecond = _sampler.ReadSingle(CounterPaths.SystemCallsPerSecond),
            InterruptsPerSecond = _sampler.ReadSingle(CounterPaths.ProcessorInterruptsPerSecondTotal),
            ProcessCount = system?.ProcessCount,
            ThreadCount = system?.ThreadCount,
            HandleCount = system?.HandleCount,
            Uptime = system?.Uptime,
            PackagePowerWatts = SumWatts(meters, PowerMeterDomain.CpuPackage),
            CoresPowerWatts = SumWatts(meters, PowerMeterDomain.CpuCores),
            LogicalProcessors = BuildLogicalProcessors(usage, performance, frequency),
        };
    }

    private static IReadOnlyList<LogicalProcessorUsage> BuildLogicalProcessors(
        IReadOnlyDictionary<string, double> usage,
        IReadOnlyDictionary<string, double> performance,
        IReadOnlyDictionary<string, double> frequency) =>
        usage
            .Select(x => (Key: ParseProcessorKey(x.Key), Instance: x.Key, Usage: x.Value))
            .Where(x => x.Key is not null)
            .OrderBy(x => x.Key!.Value.Group)
            .ThenBy(x => x.Key!.Value.Number)
            .Select((x, index) => new LogicalProcessorUsage(
                index,
                Math.Clamp(x.Usage, 0, 100),
                Lookup(frequency, x.Instance) * Lookup(performance, x.Instance) / 100))
            .ToArray();

    /// <summary>
    /// <c>0,3</c> のような「グループ,番号」のインスタンス名を読み解く。<c>_Total</c> を含むものは null。
    /// </summary>
    private static (int Group, int Number)? ParseProcessorKey(string instance)
    {
        var comma = instance.IndexOf(',');
        if (comma <= 0
            || !int.TryParse(instance.AsSpan(0, comma), NumberStyles.None, CultureInfo.InvariantCulture, out var group)
            || !int.TryParse(instance.AsSpan(comma + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            return null;
        }

        return (group, number);
    }

    private static double? SumWatts(IReadOnlyList<PowerMeterReading> meters, PowerMeterDomain domain)
    {
        var matching = meters.Where(m => m.Domain == domain).ToArray();
        return matching.Length > 0 ? matching.Sum(m => m.Watts) : null;
    }

    private static double? Lookup(IReadOnlyDictionary<string, double> values, string instance) =>
        values.TryGetValue(instance, out var value) ? value : null;

    private static double? Clamp(double? percent) => percent is { } value ? Math.Clamp(value, 0, 100) : null;
}

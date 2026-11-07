using Yamori.Core.Processes;
using Yamori.Core.Realtime.Counters;

namespace Yamori.Core.Realtime.Collectors;

/// <summary>
/// プロセスのパフォーマンスカウンターと GPU エンジンのカウンターから、アプリごとの CPU・GPU・入出力の使用量を集める。
/// </summary>
/// <remarks>
/// パフォーマンスカウンターはプロセスを開かずに読めるため、保護されたプロセスや他のユーザーのプロセスでもエラーにならない。
/// 値を読めなかったプロセスは集計に含めない。
/// </remarks>
public sealed class AppRealtimeCollector
{
    /// <summary>種類ごとに並べるアプリの数。</summary>
    public const int TopCount = 5;

    private readonly IPerformanceCounterSampler _sampler;
    private readonly int _logicalProcessorCount;

    /// <summary>
    /// カウンターの読み出し元と、論理プロセッサの数を指定して作る。
    /// </summary>
    /// <param name="sampler">カウンターの読み出し元。</param>
    /// <param name="logicalProcessorCount">論理プロセッサの数。プロセスの CPU 使用率を PC 全体の割合にするために使う。</param>
    /// <exception cref="ArgumentNullException"><paramref name="sampler"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="logicalProcessorCount"/> が 0 以下。</exception>
    public AppRealtimeCollector(IPerformanceCounterSampler sampler, int logicalProcessorCount)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(logicalProcessorCount);
        _sampler = sampler;
        _logicalProcessorCount = logicalProcessorCount;
    }

    /// <summary>
    /// アプリごとの今の使用量を集める。
    /// </summary>
    /// <returns>CPU・GPU・入出力のそれぞれで多く使っているアプリ。</returns>
    public AppsRealtime Collect()
    {
        var processes = _sampler.IsAvailable(CounterPaths.ProcessV2ProcessorTime) ? ReadProcessV2() : ReadLegacyProcess();
        if (processes.Count == 0)
        {
            return AppsRealtime.Empty;
        }

        var gpu = ReadGpuByProcess();
        var activities = processes.Values.Select(p => p with { GpuPercent = gpu.GetValueOrDefault(p.ProcessId) });
        var apps = ProcessActivityAggregator.GroupByApp(activities);

        return new AppsRealtime(
            ProcessActivityAggregator.Top(apps, a => a.CpuPercent, TopCount),
            ProcessActivityAggregator.Top(apps, a => a.GpuPercent, TopCount),
            ProcessActivityAggregator.Top(apps, a => a.IoBytesPerSecond, TopCount));
    }

    /// <summary>
    /// <c>\Process V2</c>（インスタンス名に PID を含む）から読む。
    /// </summary>
    private Dictionary<int, ProcessActivity> ReadProcessV2()
    {
        var cpu = _sampler.Read(CounterPaths.ProcessV2ProcessorTime);
        var read = _sampler.Read(CounterPaths.ProcessV2IoReadBytesPerSecond);
        var write = _sampler.Read(CounterPaths.ProcessV2IoWriteBytesPerSecond);

        var result = new Dictionary<int, ProcessActivity>();
        foreach (var (instance, value) in cpu)
        {
            if (ProcessCounterInstance.TryParseNameAndId(instance) is not { } parsed || parsed.ProcessId == 0)
            {
                continue;
            }

            result[parsed.ProcessId] = Create(parsed.ProcessId, parsed.Name, value, read.GetValueOrDefault(instance), write.GetValueOrDefault(instance));
        }

        return result;
    }

    /// <summary>
    /// <c>\Process V2</c> がない古い Windows で、<c>\Process</c> と <c>ID Process</c> から読む。
    /// </summary>
    private Dictionary<int, ProcessActivity> ReadLegacyProcess()
    {
        var ids = _sampler.Read(CounterPaths.ProcessId);
        var cpu = _sampler.Read(CounterPaths.ProcessProcessorTime);
        var read = _sampler.Read(CounterPaths.ProcessIoReadBytesPerSecond);
        var write = _sampler.Read(CounterPaths.ProcessIoWriteBytesPerSecond);

        var result = new Dictionary<int, ProcessActivity>();
        foreach (var (instance, value) in cpu)
        {
            if (!ids.TryGetValue(instance, out var id) || id is <= 0 or > int.MaxValue)
            {
                continue;
            }

            var processId = (int)id;
            var name = ProcessCounterInstance.StripDuplicateIndex(instance);
            result[processId] = Create(processId, name, value, read.GetValueOrDefault(instance), write.GetValueOrDefault(instance));
        }

        return result;
    }

    /// <summary>
    /// GPU エンジンの使用率をプロセスごとにまとめる。タスク マネージャーと同じく、そのプロセスが最も使っているエンジンの値にする。
    /// </summary>
    private Dictionary<int, double> ReadGpuByProcess() =>
        _sampler.Read(CounterPaths.GpuEngineUtilization)
            .Select(x => (ProcessId: ProcessCounterInstance.TryParseGpuProcessId(x.Key), Engine: GpuCounterInstance.TryParse(x.Key), x.Value))
            .Where(x => x.ProcessId is not null && x.Engine is { EngineIndex: not null })
            .GroupBy(x => x.ProcessId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(x => (x.Engine!.Luid, x.Engine.EngineIndex))
                    .Max(e => Math.Clamp(e.Sum(x => x.Value), 0, 100)));

    private ProcessActivity Create(int processId, string name, double processorTime, double readBytes, double writeBytes) =>
        new(
            processId,
            name,
            Math.Clamp(processorTime / _logicalProcessorCount, 0, 100),
            0,
            Math.Max(0, readBytes),
            Math.Max(0, writeBytes));
}

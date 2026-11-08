using Yamori.Core.Processes;

namespace Yamori.Core.Realtime.Counters;

/// <summary>
/// プロセスのパフォーマンスカウンターから、プロセスごとの CPU と入出力の使用量を読む（GPU は含めない）。
/// </summary>
/// <remarks>
/// パフォーマンスカウンターはプロセスを開かずに読めるため、保護されたプロセスや他のユーザーのプロセスでもエラーにならない。
/// 値を読めなかったプロセスは含めない。取り込み（<see cref="IPerformanceCounterSampler.Collect"/>）は呼び出し側が行う。
/// </remarks>
public sealed class ProcessCounterReader
{
    private readonly IPerformanceCounterSampler _sampler;
    private readonly int _logicalProcessorCount;

    /// <summary>
    /// カウンターの読み出し元と、論理プロセッサの数を指定して作る。
    /// </summary>
    /// <param name="sampler">カウンターの読み出し元。</param>
    /// <param name="logicalProcessorCount">論理プロセッサの数。プロセスの CPU 使用率を PC 全体の割合にするために使う。</param>
    /// <exception cref="ArgumentNullException"><paramref name="sampler"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="logicalProcessorCount"/> が 0 以下。</exception>
    public ProcessCounterReader(IPerformanceCounterSampler sampler, int logicalProcessorCount)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(logicalProcessorCount);
        _sampler = sampler;
        _logicalProcessorCount = logicalProcessorCount;
    }

    /// <summary>
    /// プロセスごとの今の使用量を読む。<c>\Process V2</c> があればそれを、なければ <c>\Process</c> を使う。
    /// </summary>
    /// <returns>PID をキーにした使用量。GPU 使用率は 0。</returns>
    public IReadOnlyDictionary<int, ProcessActivity> Read() =>
        _sampler.IsAvailable(CounterPaths.ProcessV2ProcessorTime) ? ReadProcessV2() : ReadLegacyProcess();

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

    private ProcessActivity Create(int processId, string name, double processorTime, double readBytes, double writeBytes) =>
        new(
            processId,
            name,
            Math.Clamp(processorTime / _logicalProcessorCount, 0, 100),
            0,
            Math.Max(0, readBytes),
            Math.Max(0, writeBytes));
}

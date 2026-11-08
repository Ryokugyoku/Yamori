using Yamori.Core.Processes;
using Yamori.Core.Realtime.Counters;

namespace Yamori.Core.AppUsage.Sampling;

/// <summary>
/// パフォーマンスカウンターから、PC 全体とアプリごとの CPU 使用率を測る。
/// </summary>
/// <remarks>
/// PC 全体の値は負荷時間の分析（<c>cpu/.../load/total</c>）と同じ <c>% Processor Utility</c>（なければ <c>% Processor Time</c>）、
/// アプリの値はリアルタイム表示と同じ <c>% Processor Time</c> を PC 全体の割合にしたもの。アプリへのまとめ方は
/// <see cref="ProcessActivityAggregator.GroupByApp"/> と同じ（同じ名前のプロセスをまとめ、Idle・System などは除く）。
/// 測るたびに取り込むため、値は前回の測定からの平均になる。スレッドセーフではない。
/// </remarks>
public sealed class CounterAppCpuSampler : IAppCpuSampler, IDisposable
{
    private readonly IPerformanceCounterSampler _sampler;
    private readonly ProcessCounterReader _processes;

    /// <summary>
    /// カウンターの読み出し元と論理プロセッサの数を指定して作る。読み出し元はこのインスタンスが破棄する。
    /// </summary>
    /// <param name="sampler">カウンターの読み出し元（このインスタンス専用のもの）。</param>
    /// <param name="logicalProcessorCount">論理プロセッサの数。</param>
    /// <exception cref="ArgumentNullException"><paramref name="sampler"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="logicalProcessorCount"/> が 0 以下。</exception>
    public CounterAppCpuSampler(IPerformanceCounterSampler sampler, int logicalProcessorCount)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        _sampler = sampler;
        _processes = new ProcessCounterReader(sampler, logicalProcessorCount);
    }

    /// <inheritdoc />
    public AppCpuSample? Sample(DateTimeOffset nowUtc)
    {
        _sampler.Collect();

        var total = _sampler.IsAvailable(CounterPaths.ProcessorUtilityTotal)
            ? _sampler.ReadSingle(CounterPaths.ProcessorUtilityTotal)
            : _sampler.ReadSingle(CounterPaths.ProcessorTimeTotal);
        var processes = _processes.Read();
        if (total is not { } totalPercent || !double.IsFinite(totalPercent) || processes.Count == 0)
        {
            return null;
        }

        var apps = ProcessActivityAggregator.GroupByApp(processes.Values)
            .Where(a => a.CpuPercent > 0)
            .Select(a => new AppCpuShare(a.Name, a.CpuPercent))
            .ToArray();
        return new AppCpuSample(nowUtc, Math.Clamp(totalPercent, 0, 100), apps);
    }

    /// <summary>
    /// カウンターの読み出し元を破棄する。
    /// </summary>
    public void Dispose() => _sampler.Dispose();
}

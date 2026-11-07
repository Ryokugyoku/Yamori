using Yamori.Core.Realtime.Counters;
using Yamori.Core.Realtime.Gpu;

namespace Yamori.Core.Realtime.Collectors;

/// <summary>
/// GPU のパフォーマンスカウンターとドライバーのセンサーの値から、GPU ごとの今の状態を集める。
/// </summary>
public sealed class GpuRealtimeCollector
{
    private readonly IPerformanceCounterSampler _sampler;

    /// <summary>
    /// カウンターの読み出し元を指定して作る。
    /// </summary>
    /// <param name="sampler">カウンターの読み出し元。</param>
    /// <exception cref="ArgumentNullException"><paramref name="sampler"/> が null。</exception>
    public GpuRealtimeCollector(IPerformanceCounterSampler sampler)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        _sampler = sampler;
    }

    /// <summary>
    /// GPU ごとの今の状態を集める。
    /// </summary>
    /// <param name="telemetry">ドライバーが報告しているセンサーの値。ハードウェアの GPU だけを含む。</param>
    /// <returns>GPU ごとの状態。センサーの値を読めた GPU を、その順に並べる。読めなかった場合は、カウンターに現れた GPU。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="telemetry"/> が null。</exception>
    public IReadOnlyList<GpuRealtime> Collect(IReadOnlyList<GpuTelemetry> telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        var engines = ReadEngines();
        var dedicated = ReadByLuid(CounterPaths.GpuDedicatedUsage);
        var shared = ReadByLuid(CounterPaths.GpuSharedUsage);
        var committed = ReadByLuid(CounterPaths.GpuTotalCommitted);

        // センサーの値を読めない古い Windows では、カウンターに現れた GPU を番号で呼ぶ。
        var adapters = telemetry.Count > 0
            ? telemetry.Select(t => (t.Luid, t.Name, Telemetry: (GpuTelemetry?)t)).ToArray()
            : engines.Keys.Union(dedicated.Keys).Order()
                .Select((luid, i) => (Luid: luid, Name: $"GPU {i}", Telemetry: (GpuTelemetry?)null)).ToArray();

        return adapters
            .Select(a =>
            {
                var usages = engines.TryGetValue(a.Luid, out var list) ? list : [];
                return new GpuRealtime(a.Luid, a.Name)
                {
                    UsagePercent = usages.Count > 0 ? usages[0].UsagePercent : null,
                    Engines = usages,
                    DedicatedUsedBytes = Lookup(dedicated, a.Luid),
                    SharedUsedBytes = Lookup(shared, a.Luid),
                    TotalCommittedBytes = Lookup(committed, a.Luid),
                    Telemetry = a.Telemetry,
                };
            })
            .ToArray();
    }

    /// <summary>
    /// エンジンの使用率を GPU ごとにまとめる。同じエンジンを使うプロセスの値は足し合わせ、
    /// 同じ種類のエンジンが複数ある場合（Copy が 2 つなど）は最も使われているものの値にする。
    /// </summary>
    private Dictionary<ulong, IReadOnlyList<GpuEngineUsage>> ReadEngines() =>
        _sampler.Read(CounterPaths.GpuEngineUtilization)
            .Select(x => (Instance: GpuCounterInstance.TryParse(x.Key), Value: x.Value))
            .Where(x => x.Instance is { EngineIndex: not null, EngineType: not null })
            .GroupBy(x => x.Instance!.Luid)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<GpuEngineUsage>)g
                    .GroupBy(x => (x.Instance!.EngineIndex, x.Instance.EngineType))
                    .Select(e => (Type: e.Key.EngineType!, Usage: Math.Clamp(e.Sum(x => x.Value), 0, 100)))
                    .GroupBy(e => e.Type, StringComparer.OrdinalIgnoreCase)
                    .Select(t => new GpuEngineUsage(t.Key, t.Max(e => e.Usage)))
                    .OrderByDescending(e => e.UsagePercent)
                    .ThenBy(e => e.EngineType, StringComparer.OrdinalIgnoreCase)
                    .ToArray());

    private Dictionary<ulong, ulong> ReadByLuid(string counterPath) =>
        _sampler.Read(counterPath)
            .Select(x => (Instance: GpuCounterInstance.TryParse(x.Key), Value: x.Value))
            .Where(x => x.Instance is not null && x.Value >= 0)
            .GroupBy(x => x.Instance!.Luid)
            .ToDictionary(g => g.Key, g => (ulong)g.Sum(x => x.Value));

    private static ulong? Lookup(Dictionary<ulong, ulong> values, ulong luid) =>
        values.TryGetValue(luid, out var value) ? value : null;
}

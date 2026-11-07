using System.Globalization;
using Yamori.Core.Hardware;
using Yamori.Core.Realtime.Counters;

namespace Yamori.Core.Realtime.Collectors;

/// <summary>
/// 物理ディスクのパフォーマンスカウンターから、ディスクごとの今の読み書きの状態を集める。
/// </summary>
public sealed class DiskRealtimeCollector
{
    private const double MillisecondsPerSecond = 1000.0;

    private readonly IPerformanceCounterSampler _sampler;
    private readonly Func<IReadOnlyDictionary<int, string>> _names;
    private readonly Func<IReadOnlyDictionary<int, StorageReliability>> _reliability;

    /// <summary>
    /// カウンターの読み出し元と、ディスクの製品名・温度の取得方法を指定して作る。
    /// </summary>
    /// <param name="sampler">カウンターの読み出し元。</param>
    /// <param name="names">ディスクの番号ごとの製品名を返す処理（読み取りに時間がかかるため、キャッシュしたものを渡す）。</param>
    /// <param name="reliability">ディスクの番号ごとの温度などを返す処理（同上）。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public DiskRealtimeCollector(
        IPerformanceCounterSampler sampler,
        Func<IReadOnlyDictionary<int, string>> names,
        Func<IReadOnlyDictionary<int, StorageReliability>> reliability)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(reliability);

        _sampler = sampler;
        _names = names;
        _reliability = reliability;
    }

    /// <summary>
    /// ディスクごとの今の状態を集める。
    /// </summary>
    /// <returns>ディスクごとの状態。番号の順。</returns>
    public IReadOnlyList<DiskRealtime> Collect()
    {
        var idle = _sampler.Read(CounterPaths.DiskIdleTime);
        var readBytes = _sampler.Read(CounterPaths.DiskReadBytesPerSecond);
        var writeBytes = _sampler.Read(CounterPaths.DiskWriteBytesPerSecond);
        var reads = _sampler.Read(CounterPaths.DiskReadsPerSecond);
        var writes = _sampler.Read(CounterPaths.DiskWritesPerSecond);
        var readLatency = _sampler.Read(CounterPaths.DiskSecondsPerRead);
        var writeLatency = _sampler.Read(CounterPaths.DiskSecondsPerWrite);
        var queue = _sampler.Read(CounterPaths.DiskQueueLength);

        var instances = idle.Keys.Union(readBytes.Keys, StringComparer.OrdinalIgnoreCase)
            .Select(name => (Instance: name, Disk: ParseInstance(name)))
            .Where(x => x.Disk is not null)
            .OrderBy(x => x.Disk!.Value.Number)
            .ToArray();
        if (instances.Length == 0)
        {
            return [];
        }

        var names = _names();
        var reliability = _reliability();
        return instances
            .Select(x =>
            {
                var (number, letters) = x.Disk!.Value;
                return new DiskRealtime(number, names.GetValueOrDefault(number), letters)
                {
                    ActiveTimePercent = Lookup(idle, x.Instance) is { } idlePercent ? Math.Clamp(100 - idlePercent, 0, 100) : null,
                    ReadBytesPerSecond = Lookup(readBytes, x.Instance),
                    WriteBytesPerSecond = Lookup(writeBytes, x.Instance),
                    ReadsPerSecond = Lookup(reads, x.Instance),
                    WritesPerSecond = Lookup(writes, x.Instance),
                    ReadLatencyMilliseconds = Lookup(readLatency, x.Instance) * MillisecondsPerSecond,
                    WriteLatencyMilliseconds = Lookup(writeLatency, x.Instance) * MillisecondsPerSecond,
                    QueueLength = Lookup(queue, x.Instance),
                    TemperatureCelsius = reliability.GetValueOrDefault(number)?.TemperatureCelsius,
                };
            })
            .ToArray();
    }

    /// <summary>
    /// <c>0 C: D:</c> のようなインスタンス名を、ディスクの番号とドライブに分ける。<c>_Total</c> は null。
    /// </summary>
    /// <param name="instance">インスタンス名。</param>
    /// <returns>ディスクの番号とドライブ。読み解けない場合は null。</returns>
    public static (int Number, string DriveLetters)? ParseInstance(string instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var space = instance.IndexOf(' ');
        var numberText = space < 0 ? instance : instance[..space];
        if (!int.TryParse(numberText, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            return null;
        }

        return (number, space < 0 ? string.Empty : instance[(space + 1)..].Trim());
    }

    private static double? Lookup(IReadOnlyDictionary<string, double> values, string instance) =>
        values.TryGetValue(instance, out var value) ? value : null;
}

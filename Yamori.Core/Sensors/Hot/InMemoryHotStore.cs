using System.Collections.Concurrent;

namespace Yamori.Core.Sensors.Hot;

/// <summary>
/// センサーごとのリングバッファーで、最新値と直近の履歴を持つ <see cref="IHotStore"/>。
/// </summary>
public sealed class InMemoryHotStore : IHotStore
{
    /// <summary>既定の、センサーごとに持つ件数（1 秒間隔で 10 分）。</summary>
    public const int DefaultCapacity = 600;

    private readonly ConcurrentDictionary<SensorKey, SensorRingBuffer> _buffers = new();
    private readonly int _capacity;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// センサーごとに持つ件数と、現在時刻の提供元を指定して作る。
    /// </summary>
    /// <param name="timeProvider">現在時刻の提供元（直近の区間の基準）。</param>
    /// <param name="capacity">センサーごとに持つ件数。</param>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> が 0 以下。</exception>
    public InMemoryHotStore(TimeProvider timeProvider, int capacity = DefaultCapacity)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _timeProvider = timeProvider;
        _capacity = capacity;
    }

    /// <inheritdoc />
    public event EventHandler<IReadOnlyList<SensorReading>>? Appended;

    /// <inheritdoc />
    public void Append(IReadOnlyList<SensorReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);
        if (readings.Count == 0)
        {
            return;
        }

        foreach (var reading in readings)
        {
            _buffers.GetOrAdd(reading.Key, _ => new SensorRingBuffer(_capacity)).Add(reading);
        }

        Appended?.Invoke(this, readings);
    }

    /// <inheritdoc />
    public SensorReading? GetLatest(SensorKey key) =>
        _buffers.TryGetValue(key, out var buffer) ? buffer.Latest() : null;

    /// <inheritdoc />
    public IReadOnlyList<SensorReading> GetRecent(SensorKey key, TimeSpan window)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(window, TimeSpan.Zero);
        return _buffers.TryGetValue(key, out var buffer)
            ? buffer.Since(_timeProvider.GetUtcNow() - window)
            : [];
    }

    /// <inheritdoc />
    public void RetainOnly(IReadOnlySet<SensorKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        foreach (var key in _buffers.Keys)
        {
            if (!keys.Contains(key))
            {
                _buffers.TryRemove(key, out _);
            }
        }
    }
}

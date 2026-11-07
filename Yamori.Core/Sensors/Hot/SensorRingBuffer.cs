namespace Yamori.Core.Sensors.Hot;

/// <summary>
/// センサー 1 つ分の、固定長のリングバッファー。上限に達したら古いものから上書きする。
/// </summary>
/// <remarks>スレッドセーフ。</remarks>
public sealed class SensorRingBuffer
{
    private readonly SensorReading?[] _items;
    private readonly Lock _lock = new();
    private int _next;
    private int _count;

    /// <summary>
    /// 持てる件数を指定して作る。
    /// </summary>
    /// <param name="capacity">持てる件数。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> が 0 以下。</exception>
    public SensorRingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _items = new SensorReading?[capacity];
    }

    /// <summary>持てる件数を取得する。</summary>
    public int Capacity => _items.Length;

    /// <summary>今持っている件数を取得する。</summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _count;
            }
        }
    }

    /// <summary>
    /// 値を追加する。
    /// </summary>
    /// <param name="reading">追加する値。</param>
    /// <exception cref="ArgumentNullException"><paramref name="reading"/> が null。</exception>
    public void Add(SensorReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        lock (_lock)
        {
            _items[_next] = reading;
            _next = (_next + 1) % _items.Length;
            _count = Math.Min(_count + 1, _items.Length);
        }
    }

    /// <summary>
    /// 最後に追加した値を取得する。
    /// </summary>
    /// <returns>最後に追加した値。空なら null。</returns>
    public SensorReading? Latest()
    {
        lock (_lock)
        {
            return _count == 0 ? null : _items[(_next - 1 + _items.Length) % _items.Length];
        }
    }

    /// <summary>
    /// 指定した時刻以降の値を、追加した順に取得する。
    /// </summary>
    /// <param name="sinceUtc">この時刻（を含む）以降の値を返す。</param>
    /// <returns>該当する値。</returns>
    public IReadOnlyList<SensorReading> Since(DateTimeOffset sinceUtc)
    {
        lock (_lock)
        {
            var result = new List<SensorReading>(_count);
            var start = (_next - _count + _items.Length) % _items.Length;
            for (var i = 0; i < _count; i++)
            {
                var item = _items[(start + i) % _items.Length]!;
                if (item.TimestampUtc >= sinceUtc)
                {
                    result.Add(item);
                }
            }

            return result;
        }
    }
}

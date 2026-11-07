namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// センサーごとの前回記録した値を覚えておき、記録の間隔とデッドバンドに従って、今の値を記録するかどうかを決める。
/// </summary>
/// <remarks>
/// デッドバンドで記録を省いても、<see cref="Heartbeat"/> ごとには必ず記録する（値が変わらないのか、記録が途切れたのかを区別するため）。
/// 読めなかった値（<see cref="SensorStatus.Ok"/> 以外）は記録しない。スレッドセーフではない。
/// </remarks>
public sealed class LogDecisionFilter
{
    /// <summary>デッドバンドで記録を省いていても、必ず記録する間隔の既定値。</summary>
    public static readonly TimeSpan DefaultHeartbeat = TimeSpan.FromMinutes(1);

    private readonly Dictionary<SensorKey, (DateTimeOffset At, double Value)> _last = [];

    /// <summary>
    /// 必ず記録する間隔を指定して作る。
    /// </summary>
    /// <param name="heartbeat">必ず記録する間隔。省略時は <see cref="DefaultHeartbeat"/>。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="heartbeat"/> が 0 以下。</exception>
    public LogDecisionFilter(TimeSpan? heartbeat = null)
    {
        Heartbeat = heartbeat ?? DefaultHeartbeat;
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Heartbeat, TimeSpan.Zero, nameof(heartbeat));
    }

    /// <summary>デッドバンドで記録を省いていても、必ず記録する間隔を取得する。</summary>
    public TimeSpan Heartbeat { get; }

    /// <summary>
    /// 今の値を記録するかどうかを決め、記録するならその値を覚える。
    /// </summary>
    /// <param name="reading">今の値。</param>
    /// <param name="policy">記録の設定。</param>
    /// <returns>記録するなら true。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public bool ShouldRecord(SensorReading reading, SensorLogPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(reading);
        ArgumentNullException.ThrowIfNull(policy);

        if (!policy.Enabled || reading.Status != SensorStatus.Ok || reading.Value is not { } value)
        {
            return false;
        }

        if (_last.TryGetValue(reading.Key, out var last))
        {
            var elapsed = reading.TimestampUtc - last.At;
            if (elapsed < policy.Interval)
            {
                return false;
            }

            if (elapsed < Heartbeat && Math.Abs(value - last.Value) < policy.Deadband)
            {
                return false;
            }
        }

        _last[reading.Key] = (reading.TimestampUtc, value);
        return true;
    }

    /// <summary>
    /// 指定したキー以外の覚えている値を捨てる。
    /// </summary>
    /// <param name="keys">残すキー。</param>
    /// <exception cref="ArgumentNullException"><paramref name="keys"/> が null。</exception>
    public void RetainOnly(IReadOnlySet<SensorKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        foreach (var key in _last.Keys.Where(k => !keys.Contains(k)).ToArray())
        {
            _last.Remove(key);
        }
    }
}

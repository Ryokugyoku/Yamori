namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// RAPL のエネルギーのカウンター（32 ビットで一周する）の差分から、平均の電力を求める。
/// </summary>
public sealed class RaplEnergyMeter
{
    private readonly double _joulesPerCount;
    private uint? _previousCount;
    private long _previousTimestamp;

    /// <summary>
    /// カウンターの単位を指定して作る。
    /// </summary>
    /// <param name="joulesPerCount">1 カウントあたりのジュール（<see cref="IntelMsrDecoder.JoulesPerCount"/>）。</param>
    /// <exception cref="ArgumentOutOfRangeException">単位が 0 以下。</exception>
    public RaplEnergyMeter(double joulesPerCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(joulesPerCount, 0);
        _joulesPerCount = joulesPerCount;
    }

    /// <summary>
    /// カウンターの今の値を渡し、前回からの平均の電力を求める。
    /// </summary>
    /// <param name="energyStatus">エネルギーのカウンターの MSR の値（下位 32 ビットを使う）。</param>
    /// <param name="timestamp">読んだ時刻（<see cref="TimeProvider.GetTimestamp"/> の値）。</param>
    /// <param name="timestampFrequency">時刻の 1 秒あたりの刻み（<see cref="TimeProvider.TimestampFrequency"/>）。</param>
    /// <returns>平均の電力（W）。初めての値、または前回と同じ時刻の場合は null。</returns>
    public double? Update(ulong energyStatus, long timestamp, long timestampFrequency)
    {
        var count = (uint)energyStatus;
        var previous = _previousCount;
        var elapsedTicks = timestamp - _previousTimestamp;
        _previousCount = count;
        _previousTimestamp = timestamp;

        if (previous is not { } before || elapsedTicks <= 0 || timestampFrequency <= 0)
        {
            return null;
        }

        // 32 ビットで一周しても、符号なしの引き算で正しい差分になる。
        var delta = unchecked(count - before);
        return delta * _joulesPerCount / ((double)elapsedTicks / timestampFrequency);
    }
}

namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// IA32_APERF と IA32_MPERF の差分の比 × 定格クロックから、動いていた間の実際のクロックを求める（1 つの論理プロセッサ分）。
/// </summary>
public sealed class EffectiveClockMeter
{
    private readonly double _baseClockHertz;
    private ulong? _previousAperf;
    private ulong _previousMperf;

    /// <summary>
    /// 定格クロックを指定して作る。
    /// </summary>
    /// <param name="baseClockHertz">定格クロック（Hz。<see cref="IntelMsrDecoder.BaseClockHertz"/>）。</param>
    /// <exception cref="ArgumentOutOfRangeException">定格クロックが 0 以下。</exception>
    public EffectiveClockMeter(double baseClockHertz)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(baseClockHertz, 0);
        _baseClockHertz = baseClockHertz;
    }

    /// <summary>
    /// カウンターの今の値を渡し、前回からの実クロックを求める。
    /// </summary>
    /// <param name="aperf">IA32_APERF の値。</param>
    /// <param name="mperf">IA32_MPERF の値。</param>
    /// <returns>実クロック（Hz）。初めての値、または MPERF が進んでいない（ずっと止まっていた）場合は null。</returns>
    public double? Update(ulong aperf, ulong mperf)
    {
        var previous = _previousAperf;
        var previousMperf = _previousMperf;
        _previousAperf = aperf;
        _previousMperf = mperf;

        if (previous is not { } beforeAperf)
        {
            return null;
        }

        // 64 ビットで一周しても、符号なしの引き算で正しい差分になる。
        var deltaAperf = unchecked(aperf - beforeAperf);
        var deltaMperf = unchecked(mperf - previousMperf);
        return deltaMperf == 0 ? null : (double)deltaAperf / deltaMperf * _baseClockHertz;
    }
}

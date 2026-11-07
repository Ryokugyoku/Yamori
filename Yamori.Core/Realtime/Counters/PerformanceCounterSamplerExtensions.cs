namespace Yamori.Core.Realtime.Counters;

/// <summary>
/// <see cref="IPerformanceCounterSampler"/> の値を読みやすくする補助メソッド。
/// </summary>
public static class PerformanceCounterSamplerExtensions
{
    /// <summary>
    /// インスタンスが 1 つだけのカウンター（<c>_Total</c> や <c>\Memory\...</c> など）の値を読み出す。
    /// </summary>
    /// <param name="sampler">カウンターの読み出し元。</param>
    /// <param name="counterPath">英語名のカウンターのパス。</param>
    /// <returns>値。読めない場合は null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sampler"/> が null。</exception>
    public static double? ReadSingle(this IPerformanceCounterSampler sampler, string counterPath)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        var values = sampler.Read(counterPath);
        return values.Count > 0 ? values.Values.First() : null;
    }

    /// <summary>
    /// インスタンスが 1 つだけのカウンターの値を、0 以上の整数として読み出す。
    /// </summary>
    /// <param name="sampler">カウンターの読み出し元。</param>
    /// <param name="counterPath">英語名のカウンターのパス。</param>
    /// <returns>値。読めないか負の場合は null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sampler"/> が null。</exception>
    public static ulong? ReadSingleUInt64(this IPerformanceCounterSampler sampler, string counterPath) =>
        sampler.ReadSingle(counterPath) is >= 0 and var value ? (ulong)value : null;
}

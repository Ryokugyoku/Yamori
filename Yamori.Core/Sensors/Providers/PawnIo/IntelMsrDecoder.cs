namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// Intel の MSR の値を、温度・エネルギーの単位・定格クロックに読み解く。ハードウェアには触れない（計算だけ）。
/// </summary>
public static class IntelMsrDecoder
{
    /// <summary>TjMax が読めない（0 が返る）ときに使う値（℃）。</summary>
    public const double FallbackTjMax = 100;

    /// <summary>定格クロックの倍率の 1 あたりの周波数（100 MHz）。</summary>
    public const double BusClockHertz = 100_000_000;

    /// <summary>
    /// IA32_TEMPERATURE_TARGET から TjMax を読む。
    /// </summary>
    /// <param name="temperatureTarget">IA32_TEMPERATURE_TARGET の値。</param>
    /// <returns>TjMax（℃）。0 の場合は <see cref="FallbackTjMax"/>。</returns>
    public static double TjMax(ulong temperatureTarget) => ReportedTjMax(temperatureTarget) ?? FallbackTjMax;

    /// <summary>
    /// IA32_TEMPERATURE_TARGET から、CPU が報告している TjMax を読む（代わりの値は使わない）。
    /// </summary>
    /// <param name="temperatureTarget">IA32_TEMPERATURE_TARGET の値。</param>
    /// <returns>TjMax（℃）。報告されていない（0）場合は null。</returns>
    public static double? ReportedTjMax(ulong temperatureTarget)
    {
        var value = (temperatureTarget >> 16) & 0xFF;
        return value == 0 ? null : value;
    }

    /// <summary>
    /// IA32_TEMPERATURE_TARGET から、熱制御（TCC）の作動温度の TjMax からのずれを読む。
    /// </summary>
    /// <param name="temperatureTarget">IA32_TEMPERATURE_TARGET の値。</param>
    /// <returns>ずれ（℃。ビット 29:24。0〜63）。熱制御は TjMax からこの分だけ低い温度で働き始める。</returns>
    public static double TccActivationOffset(ulong temperatureTarget) => (temperatureTarget >> 24) & 0x3F;

    /// <summary>
    /// IA32_TEMPERATURE_TARGET から、熱制御（TCC）が働き始める温度を読む。
    /// </summary>
    /// <param name="temperatureTarget">IA32_TEMPERATURE_TARGET の値。</param>
    /// <returns>TjMax からずれを引いた温度（℃）。TjMax が報告されていない場合は null。</returns>
    public static double? ThrottleTemperature(ulong temperatureTarget) =>
        ReportedTjMax(temperatureTarget) is { } tjMax ? Math.Max(0, tjMax - TccActivationOffset(temperatureTarget)) : null;

    /// <summary>
    /// IA32_THERM_STATUS（コア）から温度を読む。
    /// </summary>
    /// <param name="thermStatus">IA32_THERM_STATUS の値。</param>
    /// <param name="tjMax">TjMax（℃）。</param>
    /// <returns>温度（℃）。読み取り値が有効でない（ビット 31 が 0）場合は null。</returns>
    public static double? CoreTemperature(ulong thermStatus, double tjMax) =>
        (thermStatus & 0x8000_0000) == 0 ? null : tjMax - DigitalReadout(thermStatus);

    /// <summary>
    /// IA32_PACKAGE_THERM_STATUS から温度を読む（パッケージの読み取り値には有効のビットがない）。
    /// </summary>
    /// <param name="packageThermStatus">IA32_PACKAGE_THERM_STATUS の値。</param>
    /// <param name="tjMax">TjMax（℃）。</param>
    /// <returns>温度（℃）。</returns>
    public static double PackageTemperature(ulong packageThermStatus, double tjMax) =>
        tjMax - DigitalReadout(packageThermStatus);

    /// <summary>
    /// MSR_RAPL_POWER_UNIT から、エネルギーのカウンターの 1 あたりのジュールを読む。
    /// </summary>
    /// <param name="powerUnit">MSR_RAPL_POWER_UNIT の値。</param>
    /// <returns>1 カウントあたりのジュール（1 / 2^ESU）。</returns>
    public static double JoulesPerCount(ulong powerUnit) => 1.0 / (1UL << (int)((powerUnit >> 8) & 0x1F));

    /// <summary>
    /// MSR_PLATFORM_INFO から定格クロックを読む。
    /// </summary>
    /// <param name="platformInfo">MSR_PLATFORM_INFO の値。</param>
    /// <returns>定格クロック（Hz）。倍率が 0 の場合は null。</returns>
    public static double? BaseClockHertz(ulong platformInfo)
    {
        var ratio = (platformInfo >> 8) & 0xFF;
        return ratio == 0 ? null : ratio * BusClockHertz;
    }

    /// <summary>
    /// IA32_THERM_STATUS か IA32_PACKAGE_THERM_STATUS から、今かかっている制限の印を読む。
    /// 履歴のビット（Log。書き込まないと消えない）は使わず、今の状態のビットだけを読む。
    /// </summary>
    /// <param name="thermStatus">IA32_THERM_STATUS か IA32_PACKAGE_THERM_STATUS の値。</param>
    /// <returns>今かかっている制限。</returns>
    public static IntelThermalLimits ThermalLimits(ulong thermStatus)
    {
        var limits = IntelThermalLimits.None;
        if ((thermStatus & (1UL << 0)) != 0)
        {
            limits |= IntelThermalLimits.Thermal;
        }

        if ((thermStatus & (1UL << 2)) != 0)
        {
            limits |= IntelThermalLimits.Prochot;
        }

        if ((thermStatus & (1UL << 4)) != 0)
        {
            limits |= IntelThermalLimits.CriticalTemperature;
        }

        if ((thermStatus & (1UL << 10)) != 0)
        {
            limits |= IntelThermalLimits.PowerLimit;
        }

        if ((thermStatus & (1UL << 12)) != 0)
        {
            limits |= IntelThermalLimits.CurrentLimit;
        }

        if ((thermStatus & (1UL << 14)) != 0)
        {
            limits |= IntelThermalLimits.CrossDomainLimit;
        }

        return limits;
    }

    private static double DigitalReadout(ulong status) => (status >> 16) & 0x7F;
}

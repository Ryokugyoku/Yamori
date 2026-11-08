namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// IA32_THERM_STATUS（コア）と IA32_PACKAGE_THERM_STATUS（パッケージ）の、今かかっている制限の印（Intel SDM のビットの意味）。
/// </summary>
/// <remarks>パッケージの MSR には、電流の制限とドメインをまたぐ制限のビットはない（0 のまま）。</remarks>
[Flags]
public enum IntelThermalLimits
{
    /// <summary>制限はかかっていない。</summary>
    None = 0,

    /// <summary>温度が熱制御（TCC）の作動温度に達し、熱で性能を落としている（ビット 0）。</summary>
    Thermal = 1 << 0,

    /// <summary>ほかの部品（マザーボードの VRM など）が PROCHOT# か FORCEPR# で性能を落とさせている（ビット 2）。</summary>
    Prochot = 1 << 2,

    /// <summary>危険な温度に達している（ビット 4）。</summary>
    CriticalTemperature = 1 << 4,

    /// <summary>電力の上限で、OS が求めた性能より下で動いている（ビット 10）。</summary>
    PowerLimit = 1 << 10,

    /// <summary>電流の上限で性能を落としている（コアだけ。ビット 12）。</summary>
    CurrentLimit = 1 << 12,

    /// <summary>ほかのドメイン（GPU など）の制限につられて性能を落としている（コアだけ。ビット 14）。</summary>
    CrossDomainLimit = 1 << 14,
}

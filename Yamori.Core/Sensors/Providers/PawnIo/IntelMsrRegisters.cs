namespace Yamori.Core.Sensors.Providers.PawnIo;

/// <summary>
/// Yamori が読む Intel の MSR と、読んでよい MSR の許可リスト。番号は PawnIO.Modules の IntelMSR（読み取りの許可リスト）で確かめたもの。
/// </summary>
/// <remarks>Yamori は MSR に書き込まない。許可リストにない MSR は、PawnIO に渡す前に断る。</remarks>
public static class IntelMsrRegisters
{
    /// <summary>IA32_MPERF（定格の周期で数えるカウンター）。</summary>
    public const uint Mperf = 0xE7;

    /// <summary>IA32_APERF（実際の周期で数えるカウンター）。</summary>
    public const uint Aperf = 0xE8;

    /// <summary>MSR_PLATFORM_INFO（ビット 15:8 が定格の倍率）。</summary>
    public const uint PlatformInfo = 0xCE;

    /// <summary>IA32_THERM_STATUS（コアの温度。TjMax からの差）。</summary>
    public const uint ThermStatus = 0x19C;

    /// <summary>IA32_TEMPERATURE_TARGET（ビット 23:16 が TjMax）。</summary>
    public const uint TemperatureTarget = 0x1A2;

    /// <summary>IA32_PACKAGE_THERM_STATUS（パッケージの温度。TjMax からの差）。</summary>
    public const uint PackageThermStatus = 0x1B1;

    /// <summary>MSR_RAPL_POWER_UNIT（ビット 12:8 がエネルギーの単位）。</summary>
    public const uint RaplPowerUnit = 0x606;

    /// <summary>MSR_PKG_ENERGY_STATUS（パッケージのエネルギーの累計）。</summary>
    public const uint PackageEnergyStatus = 0x611;

    /// <summary>MSR_DRAM_ENERGY_STATUS（DRAM のエネルギーの累計）。</summary>
    public const uint DramEnergyStatus = 0x619;

    /// <summary>MSR_PP0_ENERGY_STATUS（コア部分のエネルギーの累計）。</summary>
    public const uint Pp0EnergyStatus = 0x639;

    /// <summary>Yamori が読んでよい MSR の一覧。</summary>
    public static IReadOnlySet<uint> AllowedReads { get; } = new HashSet<uint>
    {
        Mperf, Aperf, PlatformInfo, ThermStatus, TemperatureTarget, PackageThermStatus,
        RaplPowerUnit, PackageEnergyStatus, DramEnergyStatus, Pp0EnergyStatus,
    };
}

using Yamori.Core.Sensors.Providers.PawnIo;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// MSR の値をテストから決められる <see cref="IMsrReader"/>。ない MSR は読めないものとして扱う。
/// </summary>
internal sealed class FakeMsrReader : IMsrReader
{
    public List<CpuCore> Cores { get; } = [];

    /// <summary>パッケージ全体の MSR（コアを指定しない読み取りと、コア 0 での読み取りで使う）。</summary>
    public Dictionary<uint, ulong> Package { get; } = [];

    public List<CoreRegisters> PerCore { get; } = [];

    public bool Disposed { get; private set; }

    /// <summary>P コア 1 つ（コア 0）と E コア 1 つ（コア 1）の Alder Lake 風の値を作る。</summary>
    public static FakeMsrReader AlderLake()
    {
        var reader = new FakeMsrReader();
        reader.Cores.Add(new CpuCore(0, 0, 0, CpuCoreKind.Performance));
        reader.Cores.Add(new CpuCore(1, 0, 16, CpuCoreKind.Efficiency));
        reader.Package[IntelMsrRegisters.TemperatureTarget] = 100UL << 16;
        reader.Package[IntelMsrRegisters.PackageThermStatus] = 40UL << 16;
        reader.Package[IntelMsrRegisters.RaplPowerUnit] = 0x000A_0E03UL;
        reader.Package[IntelMsrRegisters.PackageEnergyStatus] = 1_000_000;
        reader.Package[IntelMsrRegisters.Pp0EnergyStatus] = 500_000;
        reader.Package[IntelMsrRegisters.DramEnergyStatus] = 0;
        reader.Package[IntelMsrRegisters.PlatformInfo] = 34UL << 8;
        foreach (var temperature in new ulong[] { 45, 50 })
        {
            var core = new CoreRegisters
            {
                [IntelMsrRegisters.TemperatureTarget] = 100UL << 16,
                [IntelMsrRegisters.ThermStatus] = 0x8000_0000UL | (temperature << 16),
                [IntelMsrRegisters.Aperf] = 10_000,
                [IntelMsrRegisters.Mperf] = 10_000,
            };
            reader.PerCore.Add(core);
        }

        return reader;
    }

    public bool TryRead(CpuCore? core, ReadOnlySpan<uint> registers, Span<ulong> values)
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
        CoreRegisters? perCore = core is null ? null : PerCore[core.Index];
        if (perCore is not null && registers.Length > 0 && registers[0] is IntelMsrRegisters.Aperf)
        {
            perCore.PinnedReads++;
        }

        for (var i = 0; i < registers.Length; i++)
        {
            if (perCore is not null && perCore.TryGetValue(registers[i], out var coreValue))
            {
                values[i] = coreValue;
            }
            else if (Package.TryGetValue(registers[i], out var packageValue) && !IsPerCoreOnly(registers[i]))
            {
                values[i] = packageValue;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public void Dispose() => Disposed = true;

    private static bool IsPerCoreOnly(uint register) =>
        register is IntelMsrRegisters.ThermStatus or IntelMsrRegisters.Aperf or IntelMsrRegisters.Mperf;

    /// <summary>コア 1 つの MSR。</summary>
    internal sealed class CoreRegisters : Dictionary<uint, ulong>
    {
        /// <summary>APERF から読んだ回数（読み取りの合図の確認用）。</summary>
        public int PinnedReads { get; set; }
    }
}

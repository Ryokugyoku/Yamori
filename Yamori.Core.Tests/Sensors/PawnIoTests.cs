using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Providers.PawnIo;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Sensors;

public class PawnIoTests
{
    [Theory]
    [InlineData(0x0064_0000UL, 100)]
    [InlineData(0x0F5A_0000UL, 90)]
    [InlineData(0UL, IntelMsrDecoder.FallbackTjMax)]
    public void TjMaxComesFromBits23To16(ulong value, double expected) =>
        Assert.Equal(expected, IntelMsrDecoder.TjMax(value));

    [Fact]
    public void TemperatureIsTjMaxMinusReadout()
    {
        // 読み取り値（ビット 22:16）= 0x2D = 45 → 100 - 45 = 55 ℃。
        Assert.Equal(55, IntelMsrDecoder.CoreTemperature(0x8000_0000UL | (0x2DUL << 16), 100));
        Assert.Equal(55, IntelMsrDecoder.PackageTemperature(0x2DUL << 16, 100));
        Assert.Equal(90, IntelMsrDecoder.PackageTemperature(0x0AUL << 16 | 0xFFFF, 100));
    }

    [Fact]
    public void CoreTemperatureWithoutValidBitIsUnknown() =>
        Assert.Null(IntelMsrDecoder.CoreTemperature(0x2DUL << 16, 100));

    [Fact]
    public void EnergyUnitAndBaseClockAreDecoded()
    {
        // Alder Lake の典型値：ESU = 14（1/16384 J）、定格の倍率 34 = 3.4 GHz。
        Assert.Equal(1.0 / 16384, IntelMsrDecoder.JoulesPerCount(0x000A_0E03UL));
        Assert.Equal(3_400_000_000, IntelMsrDecoder.BaseClockHertz(0x0000_2200UL));
        Assert.Null(IntelMsrDecoder.BaseClockHertz(0));
    }

    [Fact]
    public void RaplPowerIsEnergyDeltaOverTime()
    {
        var meter = new RaplEnergyMeter(1.0 / 16384);

        Assert.Null(meter.Update(1000, 0, 1000));
        var watts = meter.Update(1000 + (16384 * 50), 500, 1000);

        // 50 J を 0.5 秒で使った → 100 W。
        Assert.Equal(100, watts!.Value, 6);
    }

    [Fact]
    public void RaplCounterWrapAroundIsHandled()
    {
        var meter = new RaplEnergyMeter(1.0 / 16384);
        meter.Update(0xFFFF_F000UL, 0, 1000);

        // 0xFFFFF000 → 0x00001000 は 0x2000 = 8192 カウント = 0.5 J。1 秒で 0.5 W。
        var watts = meter.Update(0x1_0000_1000UL, 1000, 1000);

        Assert.Equal(0.5, watts!.Value, 6);
    }

    [Fact]
    public void RaplIgnoresSameTimestamp()
    {
        var meter = new RaplEnergyMeter(1.0 / 16384);
        meter.Update(1, 10, 1000);

        Assert.Null(meter.Update(2, 10, 1000));
    }

    [Fact]
    public void EffectiveClockIsAperfOverMperfTimesBase()
    {
        var meter = new EffectiveClockMeter(3_400_000_000);

        Assert.Null(meter.Update(1_000, 1_000));
        var clock = meter.Update(1_000 + 1_500_000, 1_000 + 1_000_000);

        Assert.Equal(5_100_000_000, clock!.Value, 1);
    }

    [Fact]
    public void EffectiveClockHandlesWrapAndIdle()
    {
        var meter = new EffectiveClockMeter(1_000_000_000);
        meter.Update(ulong.MaxValue - 99, ulong.MaxValue - 199);

        Assert.Equal(500_000_000, meter.Update(100, 200)!.Value, 1);
        Assert.Null(meter.Update(100, 200));
    }

    [Fact]
    public void CoreLayoutMarksPerformanceAndEfficiencyCores()
    {
        // P コア 2 つ（各 2 スレッド、効率クラス 1）と E コア 2 つ（効率クラス 0）。
        var buffer = Records(
            (Mask: 0b0000_0011UL, Class: 1),
            (Mask: 0b0000_1100UL, Class: 1),
            (Mask: 0b0001_0000UL, Class: 0),
            (Mask: 0b0010_0000UL, Class: 0));

        var cores = CpuCoreLayoutParser.Parse(buffer);

        Assert.Equal(4, cores.Count);
        Assert.Equal(["P-Core #0", "P-Core #1", "E-Core #2", "E-Core #3"], cores.Select(c => c.DisplayName));
        Assert.Equal([0, 2, 4, 5], cores.Select(c => (int)c.LogicalProcessor));
        Assert.Equal("core-2", cores[2].KeyName);
    }

    [Fact]
    public void NonHybridCoresAreUniform()
    {
        var cores = CpuCoreLayoutParser.Parse(Records((0b01UL, 0), (0b10UL, 0)));

        Assert.All(cores, c => Assert.Equal(CpuCoreKind.Uniform, c.Kind));
        Assert.Equal("Core #1", cores[1].DisplayName);
    }

    [Fact]
    public void AllowListContainsOnlyTheRegistersYamoriReads()
    {
        Assert.Equal(
            new uint[] { 0xE7, 0xE8, 0xCE, 0x19C, 0x1A2, 0x1B1, 0x606, 0x611, 0x619, 0x639 }.Order(),
            IntelMsrRegisters.AllowedReads.Order());
    }

    [Fact]
    public void SamplerDeclaresAndReadsIntelSensors()
    {
        var clock = new SteppingClock();
        var reader = FakeMsrReader.AlderLake();
        var sampler = new IntelMsrSensorSampler(reader, reader.Cores, clock);

        var catalog = sampler.Discover("pawnio");
        var keys = catalog.Select(d => d.Key.ToString()).ToHashSet();

        Assert.Contains("cpu/0/temperature/package", keys);
        Assert.Contains("cpu/0/temperature/core-0", keys);
        Assert.Contains("cpu/0/temperature/core-1", keys);
        Assert.Contains("cpu/0/power/package", keys);
        Assert.Contains("cpu/0/power/cores", keys);
        Assert.DoesNotContain("memory/0/power/dram", keys);
        Assert.Contains("cpu/0/clock/average-effective", keys);
        Assert.Contains("cpu/0/clock/core-1-effective", keys);
        Assert.Equal("E-Core #1", catalog.Single(d => d.Key.ToString() == "cpu/0/temperature/core-1").DisplayName);
        Assert.All(catalog.Where(d => d.Key.Name.StartsWith("core-", StringComparison.Ordinal)), d => Assert.Equal("cores", d.Group));

        // 1 秒後：パッケージは 32 J（= 32 W）、コア 0 は APERF が MPERF の 1.5 倍。
        clock.Advance(TimeSpan.FromSeconds(1));
        reader.Package[IntelMsrRegisters.PackageEnergyStatus] += 16384 * 32;
        reader.Package[IntelMsrRegisters.Pp0EnergyStatus] += 16384 * 20;
        reader.PerCore[0][IntelMsrRegisters.Aperf] += 3_000_000;
        reader.PerCore[0][IntelMsrRegisters.Mperf] += 2_000_000;
        reader.PerCore[1][IntelMsrRegisters.Aperf] += 1_000_000;
        reader.PerCore[1][IntelMsrRegisters.Mperf] += 1_000_000;

        var readings = sampler.Read(catalog.Select(d => d.Key).Append(WellKnownSensorKeys.CpuTotalLoad).ToArray(), "pawnio")
            .ToDictionary(r => r.Key.ToString());

        Assert.Equal(60, readings["cpu/0/temperature/package"].Value);
        Assert.Equal(55, readings["cpu/0/temperature/core-0"].Value);
        Assert.Equal(32, readings["cpu/0/power/package"].Value!.Value, 3);
        Assert.Equal(20, readings["cpu/0/power/cores"].Value!.Value, 3);
        Assert.Equal(5_100_000_000, readings["cpu/0/clock/core-0-effective"].Value!.Value, 1);
        Assert.Equal(3_400_000_000, readings["cpu/0/clock/core-1-effective"].Value!.Value, 1);
        Assert.Equal(4_250_000_000, readings["cpu/0/clock/average-effective"].Value!.Value, 1);
        Assert.Equal(SensorStatus.Unsupported, readings["cpu/0/load/total"].Status);
    }

    [Fact]
    public void SamplerSkipsSensorsWhoseRegistersCannotBeRead()
    {
        var reader = FakeMsrReader.AlderLake();
        reader.Package.Remove(IntelMsrRegisters.RaplPowerUnit);
        reader.PerCore[1].Remove(IntelMsrRegisters.ThermStatus);
        var sampler = new IntelMsrSensorSampler(reader, reader.Cores, new ManualClock());

        var keys = sampler.Discover("pawnio").Select(d => d.Key.ToString()).ToArray();

        Assert.DoesNotContain(keys, k => k.Contains("/power/", StringComparison.Ordinal));
        Assert.DoesNotContain("cpu/0/temperature/core-1", keys);
        Assert.Contains("cpu/0/temperature/core-0", keys);
    }

    [Fact]
    public async Task ProviderNeverOpensPawnIoWithoutConsent()
    {
        var control = new SwitchableControl();
        var opened = 0;
        var reader = FakeMsrReader.AlderLake();
        using var provider = new PawnIoProvider(control, () => { opened++; return reader; }, () => reader.Cores, () => true, new ManualClock());

        Assert.Empty(await provider.DiscoverAsync(CancellationToken.None));
        Assert.Equal(0, opened);

        control.Allowed = true;
        Assert.NotEmpty(await provider.DiscoverAsync(CancellationToken.None));
        Assert.Equal(1, opened);

        control.Allowed = false;
        Assert.Empty(await provider.DiscoverAsync(CancellationToken.None));
        Assert.True(reader.Disposed);
        var reading = Assert.Single(await provider.ReadAsync([IntelMsrSensorSampler.PackageTemperatureKey], CancellationToken.None));
        Assert.Equal(SensorStatus.TemporarilyUnavailable, reading.Status);
    }

    [Fact]
    public async Task ProviderDeclaresNothingOnNonIntelCpu()
    {
        var control = new SwitchableControl { Allowed = true };
        var opened = 0;
        using var provider = new PawnIoProvider(control, () => { opened++; return FakeMsrReader.AlderLake(); }, () => [], () => false, new ManualClock());

        Assert.Empty(await provider.DiscoverAsync(CancellationToken.None));
        Assert.Equal(0, opened);
    }

    [Fact]
    public async Task ProviderOpenFailureFailsDiscovery()
    {
        var control = new SwitchableControl { Allowed = true };
        using var provider = new PawnIoProvider(control, () => throw new DllNotFoundException(), () => [], () => true, new ManualClock());

        await Assert.ThrowsAsync<DllNotFoundException>(() => provider.DiscoverAsync(CancellationToken.None));
    }

    private static byte[] Records(params (ulong Mask, byte Class)[] cores)
    {
        const int size = 48;
        var buffer = new byte[size * cores.Length];
        for (var i = 0; i < cores.Length; i++)
        {
            var record = buffer.AsSpan(i * size, size);
            BitConverter.TryWriteBytes(record[0..], 0u);
            BitConverter.TryWriteBytes(record[4..], (uint)size);
            record[9] = cores[i].Class;
            BitConverter.TryWriteBytes(record[30..], (ushort)1);
            BitConverter.TryWriteBytes(record[32..], cores[i].Mask);
            BitConverter.TryWriteBytes(record[40..], (ushort)0);
        }

        return buffer;
    }

    /// <summary>現在時刻と時刻の刻みを、テストから一緒に進める時計。</summary>
    private sealed class SteppingClock : TimeProvider
    {
        private TimeSpan _elapsed;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public void Advance(TimeSpan delta) => _elapsed += delta;

        public override DateTimeOffset GetUtcNow() => new DateTimeOffset(2026, 11, 7, 0, 0, 0, TimeSpan.Zero) + _elapsed;

        public override long GetTimestamp() => _elapsed.Ticks;
    }

    private sealed class SwitchableControl : IExtendedSensorControl
    {
        public event EventHandler? ConsentChanged
        {
            add { }
            remove { }
        }

        public bool Allowed { get; set; }

        public bool IsConsented => Allowed;

        public bool CanUsePawnIo => Allowed;

        public ExtendedSensorState GetState(bool discoveryFailed) => Allowed ? ExtendedSensorState.Active : ExtendedSensorState.ConsentRequired;

        public SensorSettingResult SetConsent(bool enabled)
        {
            Allowed = enabled;
            return SensorSettingResult.Applied;
        }
    }
}

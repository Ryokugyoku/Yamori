using System.Globalization;
using Yamori.Core.Realtime;
using Yamori.Core.Realtime.Collectors;
using Yamori.Core.Realtime.Counters;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Display;
using Yamori.Core.Sensors.Providers.PawnIo;
using Yamori.Core.Sensors.Providers.WindowsStandard.Mappers;
using Yamori.Core.Sensors.TimeSeries;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.Sensors;

public class ThrottlingSensorTests
{
    [Fact]
    public void FlagTypeHasItsOwnTokenUnitAndDeadband()
    {
        var key = SensorKey.Parse("cpu/0/flag/prochot");

        Assert.Equal(SensorType.Flag, key.SensorType);
        Assert.Equal(SensorUnit.Flag, SensorUnits.ForType(SensorType.Flag));
        Assert.Equal(0.5, DefaultSensorLogPolicies.DeadbandFor(SensorType.Flag));
        Assert.Equal("On", SensorValueFormatter.Format(1, SensorUnit.Flag, CultureInfo.InvariantCulture));
        Assert.Equal("Off", SensorValueFormatter.Format(0, SensorUnit.Flag, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(0UL, IntelThermalLimits.None)]
    [InlineData(1UL << 0, IntelThermalLimits.Thermal)]
    [InlineData((1UL << 1) | (1UL << 3) | (1UL << 11), IntelThermalLimits.None)] // 履歴（Log）のビットだけなら制限なし
    [InlineData((1UL << 2) | (1UL << 10), IntelThermalLimits.Prochot | IntelThermalLimits.PowerLimit)]
    [InlineData((1UL << 4) | (1UL << 12) | (1UL << 14), IntelThermalLimits.CriticalTemperature | IntelThermalLimits.CurrentLimit | IntelThermalLimits.CrossDomainLimit)]
    public void DecoderReadsOnlyTheCurrentLimitBits(ulong status, IntelThermalLimits expected)
    {
        Assert.Equal(expected, IntelMsrDecoder.ThermalLimits(status | (40UL << 16) | 0x8000_0000UL));
    }

    [Fact]
    public void ReportedTjMaxIsNullWhenTheCpuDoesNotReportIt()
    {
        Assert.Equal(95, IntelMsrDecoder.ReportedTjMax(95UL << 16));
        Assert.Null(IntelMsrDecoder.ReportedTjMax(0));
        Assert.Equal(IntelMsrDecoder.FallbackTjMax, IntelMsrDecoder.TjMax(0));
    }

    [Fact]
    public void SamplerDeclaresTjMaxAndThrottlingFlags()
    {
        var reader = FakeMsrReader.AlderLake();
        var sampler = new IntelMsrSensorSampler(reader, reader.Cores, new ManualClock());

        var catalog = sampler.Discover("pawnio");
        var byKey = catalog.ToDictionary(d => d.Key);

        Assert.Equal("temperature", byKey[IntelMsrSensorSampler.TjMaxKey].Group);
        foreach (var key in new[]
        {
            IntelMsrSensorSampler.PackageThermalThrottlingKey, IntelMsrSensorSampler.ProchotKey, IntelMsrSensorSampler.PackagePowerLimitKey,
            IntelMsrSensorSampler.CriticalTemperatureKey, IntelMsrSensorSampler.CoresThermalThrottlingKey, IntelMsrSensorSampler.CoresPowerLimitKey,
            IntelMsrSensorSampler.CoresCurrentLimitKey, IntelMsrSensorSampler.CoresCrossDomainLimitKey,
        })
        {
            Assert.Equal(SensorGroups.Throttling, byKey[key].Group);
            Assert.Equal(SensorUnit.Flag, byKey[key].Unit);
        }
    }

    [Fact]
    public void SamplerReadsTjMaxAndFlagsFromThermStatus()
    {
        var reader = FakeMsrReader.AlderLake();
        // パッケージの値はコア 0 で読むため、コア 0 の TjMax をパッケージの TjMax にする。
        reader.PerCore[0][IntelMsrRegisters.TemperatureTarget] = 95UL << 16;
        var sampler = new IntelMsrSensorSampler(reader, reader.Cores, new ManualClock());
        var catalog = sampler.Discover("pawnio");

        // パッケージ：熱の制限と電力の制限。コア 1 だけ熱の制限と電流の制限。
        reader.Package[IntelMsrRegisters.PackageThermStatus] = (10UL << 16) | (1UL << 0) | (1UL << 10);
        reader.PerCore[1][IntelMsrRegisters.ThermStatus] = 0x8000_0000UL | (5UL << 16) | (1UL << 0) | (1UL << 12);

        var values = sampler.Read(catalog.Select(d => d.Key).ToArray(), "pawnio").ToDictionary(r => r.Key, r => r.Value);

        Assert.Equal(95, values[IntelMsrSensorSampler.TjMaxKey]);
        Assert.Equal(85, values[IntelMsrSensorSampler.PackageTemperatureKey]);
        Assert.Equal(1, values[IntelMsrSensorSampler.PackageThermalThrottlingKey]);
        Assert.Equal(1, values[IntelMsrSensorSampler.PackagePowerLimitKey]);
        Assert.Equal(0, values[IntelMsrSensorSampler.ProchotKey]);
        Assert.Equal(0, values[IntelMsrSensorSampler.CriticalTemperatureKey]);
        Assert.Equal(1, values[IntelMsrSensorSampler.CoresThermalThrottlingKey]);
        Assert.Equal(1, values[IntelMsrSensorSampler.CoresCurrentLimitKey]);
        Assert.Equal(0, values[IntelMsrSensorSampler.CoresPowerLimitKey]);
        Assert.Equal(0, values[IntelMsrSensorSampler.CoresCrossDomainLimitKey]);
        Assert.Equal(95, values[SensorKey.Parse("cpu/0/temperature/core-1")]);
    }

    [Fact]
    public void SamplerDoesNotDeclareTjMaxWhenTheCpuDoesNotReportIt()
    {
        var reader = FakeMsrReader.AlderLake();
        reader.PerCore[0][IntelMsrRegisters.TemperatureTarget] = 0;
        var sampler = new IntelMsrSensorSampler(reader, reader.Cores, new ManualClock());

        var keys = sampler.Discover("pawnio").Select(d => d.Key).ToArray();

        Assert.DoesNotContain(IntelMsrSensorSampler.TjMaxKey, keys);
        Assert.Contains(IntelMsrSensorSampler.PackageThermalThrottlingKey, keys);
    }

    [Fact]
    public void CoreFlagsBecomeUnavailableWhenNoCoreCanBeRead()
    {
        var reader = FakeMsrReader.AlderLake();
        var sampler = new IntelMsrSensorSampler(reader, reader.Cores, new ManualClock());
        sampler.Discover("pawnio");
        reader.PerCore[0].Remove(IntelMsrRegisters.ThermStatus);
        reader.PerCore[1].Remove(IntelMsrRegisters.ThermStatus);

        var reading = Assert.Single(sampler.Read([IntelMsrSensorSampler.CoresThermalThrottlingKey], "pawnio"));

        Assert.Equal(SensorStatus.TemporarilyUnavailable, reading.Status);
    }

    [Fact]
    public void CoolingCollectorKeepsTheThrottleReasons()
    {
        var sampler = new FakeCounterSampler()
            .Set(CounterPaths.ThermalZoneHighPrecisionTemperature, (@"\_TZ.TZ00", 3500), (@"\_TZ.TZ01", 3200))
            .Set(CounterPaths.ThermalZoneThrottleReasons, (@"\_TZ.TZ00", 1));

        var zones = new CoolingRealtimeCollector(sampler).Collect([]).ThermalZones;

        Assert.Equal(1u, zones[0].ThrottleReasons);
        Assert.True(zones[0].IsThrottling);
        Assert.Null(zones[1].ThrottleReasons);
        Assert.Null(zones[1].IsThrottling);
    }

    [Fact]
    public void ThermalZoneMapperPublishesTheThrottlingFlag()
    {
        var snapshot = FakeSnapshotSource.Snapshot() with
        {
            Cooling = new CoolingRealtime([], [new ThermalZoneReading(@"\_TZ.TZ00", 62, 80, 1), new ThermalZoneReading(@"\_TZ.TZ01", 40, 100, null)]),
        };

        var samples = new ThermalZoneSensorMapper().Map(snapshot)
            .Where(s => s.Descriptor.Key.SensorType == SensorType.Flag)
            .ToArray();

        Assert.Equal(2, samples.Length);
        Assert.All(samples, s => Assert.Equal(SensorGroups.Throttling, s.Descriptor.Group));
        Assert.Equal(1, samples[0].Value);
        Assert.Null(samples[1].Value);
    }
}

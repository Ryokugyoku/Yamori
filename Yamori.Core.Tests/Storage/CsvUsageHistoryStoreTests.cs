using Yamori.Core.Hardware;
using Yamori.Core.Monitoring;
using Yamori.Core.Storage;

namespace Yamori.Core.Tests.Storage;

public sealed class CsvUsageHistoryStoreTests : IDisposable
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 6, 23, 59, 0, TimeSpan.Zero);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "YamoriTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void RoundTripsSamplesAcrossDayBoundary()
    {
        var store = new CsvUsageHistoryStore(_directory, new UsageSampleCsvSerializer());
        store.Append(
        [
            new(HardwareComponent.Cpu, T0, 12.5),
            new(HardwareComponent.Memory, T0, 60),
            new(HardwareComponent.Cpu, T0.AddMinutes(2), 99),
        ]);

        var cpu = store.Read(HardwareComponent.Cpu, T0, T0.AddMinutes(5));

        Assert.Equal(2, Directory.GetFiles(_directory).Length);
        Assert.Equal([12.5, 99], cpu.Select(s => s.UsagePercent));
        Assert.All(cpu, s => Assert.Equal(HardwareComponent.Cpu, s.Component));
    }

    [Fact]
    public void ReadFiltersByTimeRange()
    {
        var store = new CsvUsageHistoryStore(_directory, new UsageSampleCsvSerializer());
        store.Append([new(HardwareComponent.Cpu, T0, 1), new(HardwareComponent.Cpu, T0.AddSeconds(30), 2)]);

        var result = store.Read(HardwareComponent.Cpu, T0.AddSeconds(10), T0.AddMinutes(1));

        Assert.Equal(2, Assert.Single(result).UsagePercent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not,a,sample")]
    [InlineData("2026-11-06T00:00:00.0000000+00:00,Cpu")]
    [InlineData("2026-11-06T00:00:00.0000000+00:00,Toaster,10")]
    public void SerializerRejectsBrokenLines(string line)
    {
        Assert.False(new UsageSampleCsvSerializer().TryDeserialize(line, out _));
    }

    [Fact]
    public void SerializerRoundTrips()
    {
        var serializer = new UsageSampleCsvSerializer();
        var sample = new ComponentUsageSample(HardwareComponent.Memory, T0, 33.33);

        Assert.True(serializer.TryDeserialize(serializer.Serialize(sample), out var parsed));
        Assert.Equal(sample, parsed);
    }
}

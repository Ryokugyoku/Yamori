using System.Globalization;
using Yamori.Core.Realtime;

namespace Yamori.Core.Sensors.Providers.WindowsStandard.Mappers;

/// <summary>
/// 測定結果から GPU（<c>gpu/{id}</c>）のセンサーを取り出す。
/// </summary>
/// <remarks>
/// <para>ID は PCI バス上の位置から作る（LUID は再起動で変わるため使わない）。位置が読めない場合だけ、名前と同名の中での順番を使う。</para>
/// <para>使用率はタスク マネージャーと同じ「最も使われているエンジン」の値で、ドライバーが報告する GPU コアの使用率とは別のキー（<c>load/max-engine</c>）にする。</para>
/// </remarks>
public sealed class GpuSensorMapper : ISnapshotSensorMapper
{
    private const string EnginesGroup = "engines";

    /// <inheritdoc />
    public IEnumerable<SensorSample> Map(RealtimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var samples = new List<SensorSample>();
        var sameNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var gpu in snapshot.Gpus)
        {
            var id = HardwareIds.FromPersistentId(PersistentId(gpu, sameNameCounts));
            var telemetry = gpu.Telemetry;
            var builder = new SensorSampleBuilder(HardwareKind.Gpu, id, gpu.Name)
                .Add(SensorType.Load, "max-engine", "GPU (busiest engine)", gpu.UsagePercent)
                .Add(SensorType.Load, "power-tdp", "Power (% of TDP)", telemetry?.PowerPercent)
                .Add(SensorType.Data, "dedicated-used", "Dedicated Memory Used", gpu.DedicatedUsedBytes)
                .Add(SensorType.Data, "shared-used", "Shared Memory Used", gpu.SharedUsedBytes)
                .Add(SensorType.Data, "committed", "Committed Memory", gpu.TotalCommittedBytes)
                .Add(SensorType.Data, "dedicated-total", "Dedicated Memory", telemetry?.DedicatedMemoryBytes)
                .Add(SensorType.Data, "shared-total", "Shared Memory", telemetry?.SharedMemoryBytes)
                .Add(SensorType.Temperature, "adapter", "Temperature (driver)", telemetry?.TemperatureCelsius)
                .Add(SensorType.Temperature, "adapter-max", "Temperature Limit (driver)", telemetry?.MaxTemperatureCelsius)
                .Add(SensorType.Fan, "adapter", "Fan (driver)", telemetry?.FanRpm)
                .Add(SensorType.Fan, "adapter-max", "Fan Max (driver)", telemetry?.MaxFanRpm)
                .Add(SensorType.Clock, "core", "Core Clock", UnitConversions.MegahertzToHertz(telemetry?.CoreClockMhz))
                .Add(SensorType.Clock, "core-max", "Core Clock Max", UnitConversions.MegahertzToHertz(telemetry?.MaxCoreClockMhz))
                .Add(SensorType.Clock, "memory", "Memory Clock", UnitConversions.MegahertzToHertz(telemetry?.MemoryClockMhz))
                .Add(SensorType.Clock, "memory-max", "Memory Clock Max", UnitConversions.MegahertzToHertz(telemetry?.MaxMemoryClockMhz))
                .Add(SensorType.Voltage, "core", "Core Voltage", UnitConversions.MillivoltsToVolts(telemetry?.CoreVoltageMillivolts))
                .Add(SensorType.Throughput, "memory-bandwidth", "Memory Bandwidth", telemetry?.MemoryBandwidthBytesPerSecond)
                .Add(SensorType.Throughput, "memory-bandwidth-max", "Memory Bandwidth Max", telemetry?.MaxMemoryBandwidthBytesPerSecond)
                .Add(SensorType.Throughput, "pcie-bandwidth", "PCIe Bandwidth", telemetry?.PcieBandwidthBytesPerSecond)
                .Add(SensorType.Throughput, "pcie-bandwidth-max", "PCIe Bandwidth Max", telemetry?.MaxPcieBandwidthBytesPerSecond);

            foreach (var engine in gpu.Engines)
            {
                var name = SensorKeyTokens.NormalizeSegment(engine.EngineType);
                if (name.Length > 0)
                {
                    builder.Add(SensorType.Load, $"engine-{name}", engine.EngineType, engine.UsagePercent, EnginesGroup);
                }
            }

            samples.AddRange(builder.Samples);
        }

        return samples;
    }

    private static string PersistentId(GpuRealtime gpu, Dictionary<string, int> sameNameCounts)
    {
        if (gpu.Telemetry?.PciLocation is { Length: > 0 } location)
        {
            return "pci:" + location;
        }

        // 最後の手段。同じ名前の GPU が複数あり、位置も読めない場合だけ順番が入る。
        var ordinal = sameNameCounts.GetValueOrDefault(gpu.Name);
        sameNameCounts[gpu.Name] = ordinal + 1;
        return string.Create(CultureInfo.InvariantCulture, $"name:{gpu.Name}#{ordinal}");
    }
}

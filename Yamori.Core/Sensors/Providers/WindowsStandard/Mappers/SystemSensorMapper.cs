using Yamori.Core.Realtime;

namespace Yamori.Core.Sensors.Providers.WindowsStandard.Mappers;

/// <summary>
/// 測定結果から Windows 全体（<c>system/0</c>）のセンサーを取り出す。
/// </summary>
public sealed class SystemSensorMapper : ISnapshotSensorMapper
{
    private const string HardwareName = "System";

    /// <inheritdoc />
    public IEnumerable<SensorSample> Map(RealtimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var cpu = snapshot.Cpu;
        return new SensorSampleBuilder(HardwareKind.System, HardwareIds.Singleton, HardwareName)
            .Add(SensorType.Count, "processes", "Processes", cpu.ProcessCount)
            .Add(SensorType.Count, "threads", "Threads", cpu.ThreadCount)
            .Add(SensorType.Count, "handles", "Handles", cpu.HandleCount)
            .Add(SensorType.Duration, "uptime", "Uptime", cpu.Uptime?.TotalSeconds)
            .Add(SensorType.Power, "platform", "Platform (RAPL)", CpuSensorMapper.SumWatts(snapshot.Power.Meters, PowerMeterDomain.Platform))
            .Samples;
    }
}

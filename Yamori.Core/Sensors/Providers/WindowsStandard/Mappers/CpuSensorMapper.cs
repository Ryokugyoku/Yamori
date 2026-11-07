using System.Globalization;
using Yamori.Core.Realtime;

namespace Yamori.Core.Sensors.Providers.WindowsStandard.Mappers;

/// <summary>
/// 測定結果から CPU（<c>cpu/0</c>）のセンサーを取り出す。
/// </summary>
/// <remarks>
/// 標準 API の値はすべてのソケットの合計・平均のため、ソケット 0 にまとめる。
/// クロックは性能の割合からの推定値で、MSR から読む実クロックとは別のキー（<c>…-estimated</c>）にする。
/// </remarks>
public sealed class CpuSensorMapper : ISnapshotSensorMapper
{
    private const string HardwareName = "CPU";
    private const string ThreadsGroup = "threads";

    /// <inheritdoc />
    public IEnumerable<SensorSample> Map(RealtimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var cpu = snapshot.Cpu;
        var builder = new SensorSampleBuilder(HardwareKind.Cpu, HardwareIds.FromSocket(0), HardwareName)
            .Add(SensorType.Load, "total", "CPU Total", cpu.UsagePercent)
            .Add(SensorType.Load, "processor-time", "Processor Time", cpu.ProcessorTimePercent)
            .Add(SensorType.Load, "user", "User", cpu.UserPercent)
            .Add(SensorType.Load, "privileged", "Privileged", cpu.PrivilegedPercent)
            .Add(SensorType.Load, "interrupt", "Interrupt", cpu.InterruptPercent)
            .Add(SensorType.Load, "dpc", "DPC", cpu.DpcPercent)
            .Add(SensorType.Clock, "average-estimated", "Average Clock (estimated)", UnitConversions.MegahertzToHertz(cpu.ClockMhz))
            .Add(SensorType.Clock, "base", "Base Clock", UnitConversions.MegahertzToHertz(cpu.BaseClockMhz))
            .Add(SensorType.Level, "performance", "Performance", cpu.PerformancePercent)
            .Add(SensorType.Level, "performance-limit", "Performance Limit", cpu.PerformanceLimitPercent)
            .Add(SensorType.Count, "queue-length", "Processor Queue Length", cpu.QueueLength)
            .Add(SensorType.Rate, "context-switches", "Context Switches", cpu.ContextSwitchesPerSecond)
            .Add(SensorType.Rate, "system-calls", "System Calls", cpu.SystemCallsPerSecond)
            .Add(SensorType.Rate, "interrupts", "Interrupts", cpu.InterruptsPerSecond)
            .Add(SensorType.Power, "package", "Package (RAPL)", cpu.PackagePowerWatts)
            .Add(SensorType.Power, "cores", "Cores (RAPL)", cpu.CoresPowerWatts)
            .Add(SensorType.Power, "graphics", "Graphics (RAPL)", SumWatts(snapshot.Power.Meters, PowerMeterDomain.CpuGraphics));

        foreach (var processor in cpu.LogicalProcessors)
        {
            var index = processor.Index.ToString(CultureInfo.InvariantCulture);
            builder
                .Add(SensorType.Load, $"thread-{index}", $"Thread #{index}", processor.UsagePercent, ThreadsGroup)
                .Add(SensorType.Clock, $"thread-{index}-estimated", $"Thread #{index} Clock (estimated)", UnitConversions.MegahertzToHertz(processor.ClockMhz), ThreadsGroup);
        }

        return builder.Samples;
    }

    /// <summary>
    /// 電力量計のうち、指定した範囲を測っているものの合計を求める。
    /// </summary>
    /// <param name="meters">電力量計の値。</param>
    /// <param name="domain">範囲。</param>
    /// <returns>合計（W）。該当する電力量計がなければ null。</returns>
    internal static double? SumWatts(IReadOnlyList<PowerMeterReading> meters, PowerMeterDomain domain)
    {
        var matching = meters.Where(m => m.Domain == domain).ToArray();
        return matching.Length > 0 ? matching.Sum(m => m.Watts) : null;
    }
}

using Yamori.Core.Realtime;

namespace Yamori.Core.Sensors.Providers.WindowsStandard.Mappers;

/// <summary>
/// 測定結果からメインメモリ（<c>memory/0</c>）のセンサーを取り出す。
/// </summary>
public sealed class MemorySensorMapper : ISnapshotSensorMapper
{
    private const string HardwareName = "Memory";

    /// <inheritdoc />
    public IEnumerable<SensorSample> Map(RealtimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var memory = snapshot.Memory;
        return new SensorSampleBuilder(HardwareKind.Memory, HardwareIds.Singleton, HardwareName)
            .Add(SensorType.Load, "physical", "Physical Memory", memory.UsagePercent)
            .Add(SensorType.Load, "paging-file", "Paging File", memory.PagingFileUsagePercent)
            .Add(SensorType.Data, "total", "Total", memory.TotalBytes)
            .Add(SensorType.Data, "used", "Used", memory.UsedBytes)
            .Add(SensorType.Data, "available", "Available", memory.AvailableBytes)
            .Add(SensorType.Data, "committed", "Committed", memory.CommittedBytes)
            .Add(SensorType.Data, "commit-limit", "Commit Limit", memory.CommitLimitBytes)
            .Add(SensorType.Data, "commit-peak", "Commit Peak", memory.CommitPeakBytes)
            .Add(SensorType.Data, "system-cache", "System Cache", memory.SystemCacheBytes)
            .Add(SensorType.Data, "standby", "Standby", memory.StandbyBytes)
            .Add(SensorType.Data, "modified", "Modified", memory.ModifiedBytes)
            .Add(SensorType.Data, "free", "Free", memory.FreeBytes)
            .Add(SensorType.Data, "paged-pool", "Paged Pool", memory.PagedPoolBytes)
            .Add(SensorType.Data, "nonpaged-pool", "Non-paged Pool", memory.NonPagedPoolBytes)
            .Add(SensorType.Rate, "page-faults", "Page Faults", memory.PageFaultsPerSecond)
            .Add(SensorType.Rate, "pages", "Pages", memory.PagesPerSecond)
            .Add(SensorType.Power, "dram", "DRAM (RAPL)", memory.PowerWatts)
            .Samples;
    }
}

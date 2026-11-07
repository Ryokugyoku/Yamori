using Yamori.Core.Realtime;

namespace Yamori.Core.Sensors.Providers.WindowsStandard.Mappers;

/// <summary>
/// 測定結果からドライブ（<c>volume/{id}</c>）のセンサーを取り出す。ID はドライブの名前（例：<c>C:\</c>）から作る。
/// </summary>
public sealed class VolumeSensorMapper : ISnapshotSensorMapper
{
    /// <inheritdoc />
    public IEnumerable<SensorSample> Map(RealtimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var samples = new List<SensorSample>();
        foreach (var volume in snapshot.Storage.Volumes)
        {
            var hardwareName = string.IsNullOrWhiteSpace(volume.Label) ? volume.Name : $"{volume.Name} ({volume.Label})";
            samples.AddRange(new SensorSampleBuilder(HardwareKind.Volume, HardwareIds.FromPersistentId("volume:" + volume.Name), hardwareName)
                .Add(SensorType.Load, "used", "Used Space", volume.UsagePercent)
                .Add(SensorType.Data, "total", "Capacity", volume.TotalBytes)
                .Add(SensorType.Data, "free", "Free Space", volume.FreeBytes)
                .Samples);
        }

        return samples;
    }
}

using Yamori.Core.Realtime;

namespace Yamori.Core.Sensors.Providers.WindowsStandard.Mappers;

/// <summary>
/// 測定結果から物理ディスク（<c>storage/{id}</c>）のセンサーを取り出す。
/// </summary>
/// <remarks>
/// ID はシリアル番号（読めなければ PnP デバイス ID）から作る。ディスクの番号は接続の順で変わるため使わず、
/// 識別子が読めないディスクは取り出さない。
/// </remarks>
public sealed class StorageSensorMapper : ISnapshotSensorMapper
{
    private const string FallbackName = "Disk";

    private readonly Func<IReadOnlyDictionary<int, string>> _identities;

    /// <summary>
    /// ディスクの識別子の取得方法を指定して作る。
    /// </summary>
    /// <param name="identities">ディスクの番号ごとの識別子を返す処理（読み取りに時間がかかるため、キャッシュしたものを渡す）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="identities"/> が null。</exception>
    public StorageSensorMapper(Func<IReadOnlyDictionary<int, string>> identities)
    {
        ArgumentNullException.ThrowIfNull(identities);
        _identities = identities;
    }

    /// <inheritdoc />
    public IEnumerable<SensorSample> Map(RealtimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.Storage.Disks.Count == 0)
        {
            return [];
        }

        var identities = _identities();
        var samples = new List<SensorSample>();
        foreach (var disk in snapshot.Storage.Disks)
        {
            if (!identities.TryGetValue(disk.DiskNumber, out var identity))
            {
                continue;
            }

            var name = disk.Name ?? FallbackName;
            var hardwareName = disk.DriveLetters.Length > 0 ? $"{name} ({disk.DriveLetters})" : name;
            samples.AddRange(new SensorSampleBuilder(HardwareKind.Storage, HardwareIds.FromPersistentId("disk:" + identity), hardwareName)
                .Add(SensorType.Load, "active-time", "Active Time", disk.ActiveTimePercent)
                .Add(SensorType.Throughput, "read", "Read Speed", disk.ReadBytesPerSecond)
                .Add(SensorType.Throughput, "write", "Write Speed", disk.WriteBytesPerSecond)
                .Add(SensorType.Rate, "reads", "Reads", disk.ReadsPerSecond)
                .Add(SensorType.Rate, "writes", "Writes", disk.WritesPerSecond)
                .Add(SensorType.Duration, "read-latency", "Read Latency", UnitConversions.MillisecondsToSeconds(disk.ReadLatencyMilliseconds))
                .Add(SensorType.Duration, "write-latency", "Write Latency", UnitConversions.MillisecondsToSeconds(disk.WriteLatencyMilliseconds))
                .Add(SensorType.Count, "queue-length", "Queue Length", disk.QueueLength)
                .Add(SensorType.Temperature, "drive", "Temperature", disk.TemperatureCelsius)
                .Samples);
        }

        return samples;
    }
}

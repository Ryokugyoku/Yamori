using Yamori.Core.Realtime;

namespace Yamori.Core.Sensors.Providers.WindowsStandard.Mappers;

/// <summary>
/// 測定結果からネットワーク アダプター（<c>network/{id}</c>）のセンサーを取り出す。
/// </summary>
/// <remarks>
/// ID はアダプターの製品名から作る（同じ製品が複数あると Windows が「#2」などを付けて区別する）。
/// MAC アドレスなど個人を特定できる情報は使わない。
/// </remarks>
public sealed class NetworkSensorMapper : ISnapshotSensorMapper
{
    /// <inheritdoc />
    public IEnumerable<SensorSample> Map(RealtimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var samples = new List<SensorSample>();
        foreach (var network in snapshot.Networks)
        {
            samples.AddRange(new SensorSampleBuilder(HardwareKind.Network, HardwareIds.FromPersistentId("net:" + network.Name), network.Name)
                .Add(SensorType.Load, "link", "Link Usage", network.UsagePercent)
                .Add(SensorType.Throughput, "received", "Received", network.ReceivedBytesPerSecond)
                .Add(SensorType.Throughput, "sent", "Sent", network.SentBytesPerSecond)
                .Add(SensorType.Throughput, "link-speed", "Link Speed", UnitConversions.BitsToBytesPerSecond(network.LinkSpeedBitsPerSecond))
                .Samples);
        }

        return samples;
    }
}

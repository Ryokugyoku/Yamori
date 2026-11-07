using Yamori.Core.Hardware.Wmi;
using Yamori.Core.Realtime;
using Yamori.Core.Realtime.Sources;
using Yamori.Core.Sensors.Providers.WindowsStandard.Mappers;

namespace Yamori.Core.Sensors.Providers.WindowsStandard;

/// <summary>
/// 標準の構成で <see cref="WindowsStandardProvider"/> を組み立てる。
/// </summary>
public static class WindowsStandardProviderFactory
{
    private static readonly TimeSpan DiskIdentityLifetime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 既存の測定（<see cref="RealtimeSnapshotService"/>）と、すべての部品の取り出しの処理で作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <param name="timeProvider">現在時刻の提供元。</param>
    /// <returns>新しい Provider。使い終わったら破棄すること。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static WindowsStandardProvider Create(IWmiQueryExecutor wmi, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var diskIdentities = new PeriodicCache<IReadOnlyDictionary<int, string>>(
            new WmiDiskIdentitySource(wmi).ReadByDiskNumber, DiskIdentityLifetime, timeProvider);

        ISnapshotSensorMapper[] mappers =
        [
            new CpuSensorMapper(),
            new SystemSensorMapper(),
            new MemorySensorMapper(),
            new GpuSensorMapper(),
            new StorageSensorMapper(diskIdentities.Get),
            new VolumeSensorMapper(),
            new NetworkSensorMapper(),
            new BatterySensorMapper(),
            new ThermalZoneSensorMapper(),
        ];

        return new WindowsStandardProvider(
            () => RealtimeSnapshotService.CreateDefault(wmi, timeProvider),
            mappers,
            timeProvider);
    }
}

using Microsoft.Data.Sqlite;
using RocksDbSharp;
using Yamori.Core.Hardware.Wmi;
using Yamori.Core.Sensors.Collection;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Hot;
using Yamori.Core.Sensors.Master;
using Yamori.Core.Sensors.Persistence;
using Yamori.Core.Sensors.Providers;
using Yamori.Core.Sensors.Providers.PawnIo;
using Yamori.Core.Sensors.Providers.WindowsStandard;
using Yamori.Core.Sensors.Registry;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Sensors.Service;

/// <summary>
/// 標準の構成で <see cref="LocalSensorService"/> を組み立てる。
/// </summary>
public static class LocalSensorServiceFactory
{
    private const string TimeSeriesFolderName = "timeseries";
    private const string MasterFileName = "master.db";

    /// <summary>
    /// 標準 API の Provider と、時系列（RocksDB）・マスタ（SQLite）への記録を使う構成で作る。
    /// GUI のプロセスでは PawnIO を使えないため、PawnIO の Provider は登録しない（状態だけを調べる）。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <param name="priorityFilePath">優先度の設定ファイルのパス。ファイルがなければ既定の設定を使う。</param>
    /// <param name="dataDirectory">時系列とマスタを置くフォルダー。</param>
    /// <param name="timeProvider">現在時刻とタイマーの提供元。</param>
    /// <returns>新しいサービス。使い終わったら破棄すること。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static LocalSensorService Create(IWmiQueryExecutor wmi, string priorityFilePath, string dataDirectory, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        ArgumentNullException.ThrowIfNull(priorityFilePath);
        ArgumentNullException.ThrowIfNull(dataDirectory);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var owned = new List<IDisposable>();
        var extendedState = new ExtendedSensorStatusResolver(new RegistryPawnIoInstallationProbe()).ResolveInitial(canUsePawnIo: false);

        var standard = WindowsStandardProviderFactory.Create(wmi, timeProvider);
        owned.Add(standard);
        var providers = new List<ISensorProvider> { standard };
        if (extendedState == ExtendedSensorState.Active)
        {
            providers.Add(new PawnIoProvider());
        }

        var notifier = new DeviceInterfaceChangeNotifier();
        owned.Add(notifier);

        var policy = new SensorPriorityPolicy(new SensorPriorityOptionsLoader(priorityFilePath).Load());
        var registry = new SensorRegistry(providers, policy, timeProvider);
        var hotStore = new InMemoryHotStore(timeProvider);
        var loop = new SensorCollectionLoop(registry, hotStore, notifier, timeProvider);
        var persistence = TryCreatePersistence(dataDirectory, providers.Select(p => p.ProviderId).ToArray(), timeProvider, owned);

        return new LocalSensorService(registry, hotStore, loop, persistence, extendedState, timeProvider, owned);
    }

    private static SensorPersistence? TryCreatePersistence(
        string dataDirectory,
        IReadOnlyCollection<string> providerIds,
        TimeProvider timeProvider,
        List<IDisposable> owned)
    {
        MasterDatabase? master = null;
        RocksDbTimeSeriesStore? store = null;
        try
        {
            master = MasterDatabase.Open(Path.Combine(dataDirectory, MasterFileName));
            store = RocksDbTimeSeriesStore.Open(Path.Combine(dataDirectory, TimeSeriesFolderName));
        }
        catch (Exception ex) when (ex is RocksDbException or SqliteException or IOException or UnauthorizedAccessException or NotSupportedException or DllNotFoundException)
        {
            // ほかのプロセスが開いている、ディスクに書けないなど。記録なしで動く。
            store?.Dispose();
            master?.Dispose();
            return null;
        }

        owned.Add(store);
        owned.Add(master);

        var sensors = new SqliteSensorRepository(master);
        var retention = RetentionPolicy.Default;
        return new SensorPersistence(
            store,
            new TimeSeriesRecorder(store, new LogDecisionFilter()),
            new SensorCatalogSynchronizer(new SqliteHardwareRepository(master), sensors, new SqliteSourceRepository(master)),
            sensors,
            new SqliteEventLogRepository(master),
            new RollupJob(store, retention),
            new RetentionJob(store, retention),
            providerIds,
            timeProvider);
    }
}

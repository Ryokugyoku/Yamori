using Microsoft.Data.Sqlite;
using RocksDbSharp;
using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Detection;
using Yamori.Core.Hardware.Wmi;
using Yamori.Core.Sensors.Collection;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Hot;
using Yamori.Core.Sensors.Master;
using Yamori.Core.Sensors.Persistence;
using Yamori.Core.Sensors.Providers;
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
    /// Helper のプロセスで使う構成で作る。標準 API と拡張センサー（PawnIO）の Provider を使い、時系列（RocksDB）・マスタ（SQLite）に記録する。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <param name="priorityFilePath">優先度の設定ファイルのパス。ファイルがなければ既定の設定を使う。</param>
    /// <param name="dataDirectory">時系列とマスタを置くフォルダー。</param>
    /// <param name="extended">拡張センサーの判断と同意の扱い。</param>
    /// <param name="extendedProvider">拡張センサーの Provider。使わない場合は null。同意の有無は Provider の中で確かめること。</param>
    /// <param name="timeProvider">現在時刻とタイマーの提供元。</param>
    /// <param name="createAnomalyRules">異常の判定の規則の作り方（記録の読み方を受け取る）。マスタを開けたときだけ、異常を見張る（一覧と設定はマスタに置く）。</param>
    /// <returns>新しいサービス。使い終わったら破棄すること。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="extendedProvider"/> 以外の引数が null。</exception>
    public static LocalSensorService CreateForHelper(
        IWmiQueryExecutor wmi,
        string priorityFilePath,
        string dataDirectory,
        IExtendedSensorControl extended,
        ISensorProvider? extendedProvider,
        TimeProvider timeProvider,
        Func<AnomalyHistoryReader, IReadOnlyList<IAnomalyRule>> createAnomalyRules)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        ArgumentNullException.ThrowIfNull(priorityFilePath);
        ArgumentNullException.ThrowIfNull(dataDirectory);
        ArgumentNullException.ThrowIfNull(extended);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(createAnomalyRules);

        var owned = new List<IDisposable>();
        var standard = WindowsStandardProviderFactory.Create(wmi, timeProvider);
        owned.Add(standard);
        var providers = new List<ISensorProvider> { standard };
        if (extendedProvider is not null)
        {
            providers.Add(extendedProvider);
            if (extendedProvider is IDisposable disposable)
            {
                owned.Add(disposable);
            }
        }

        var (persistence, master) = TryCreatePersistence(dataDirectory, providers.Select(p => p.ProviderId).ToArray(), timeProvider, owned);
        var anomalies = master is null || persistence is null
            ? null
            : AnomalyMonitoring.Create(master, createAnomalyRules(persistence.GetHistory), timeProvider);
        return Assemble(providers, priorityFilePath, persistence, anomalies, extended, timeProvider, owned);
    }

    /// <summary>
    /// GUI のプロセスで、Helper が動いていない間の代わりの表示に使う構成で作る。標準 API だけを読み、記録はしない。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <param name="priorityFilePath">優先度の設定ファイルのパス。ファイルがなければ既定の設定を使う。</param>
    /// <param name="timeProvider">現在時刻とタイマーの提供元。</param>
    /// <returns>新しいサービス。使い終わったら破棄すること。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static LocalSensorService CreateDisplayOnly(IWmiQueryExecutor wmi, string priorityFilePath, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        ArgumentNullException.ThrowIfNull(priorityFilePath);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var owned = new List<IDisposable>();
        var standard = WindowsStandardProviderFactory.Create(wmi, timeProvider);
        owned.Add(standard);
        var extended = new OutsideHelperExtendedSensorControl(new RegistryPawnIoInstallationProbe());
        return Assemble([standard], priorityFilePath, persistence: null, anomalies: null, extended, timeProvider, owned);
    }

    private static LocalSensorService Assemble(
        IReadOnlyList<ISensorProvider> providers,
        string priorityFilePath,
        SensorPersistence? persistence,
        AnomalyMonitoring? anomalies,
        IExtendedSensorControl extended,
        TimeProvider timeProvider,
        List<IDisposable> owned)
    {
        var devices = new DeviceInterfaceChangeNotifier();
        owned.Add(devices);
        var notifier = new ConsentAwareChangeNotifier(devices, extended);
        owned.Add(notifier);

        var policy = new SensorPriorityPolicy(new SensorPriorityOptionsLoader(priorityFilePath).Load());
        var registry = new SensorRegistry(providers, policy, timeProvider);
        var hotStore = new InMemoryHotStore(timeProvider);
        var loop = new SensorCollectionLoop(registry, hotStore, notifier, timeProvider);

        // 通知を先に止めてから、Provider とストアを閉じる。
        owned.Reverse();
        return new LocalSensorService(registry, hotStore, loop, persistence, extended, timeProvider, owned, anomalies);
    }

    private static (SensorPersistence? Persistence, MasterDatabase? Master) TryCreatePersistence(
        string dataDirectory,
        IReadOnlyCollection<string> providerIds,
        TimeProvider timeProvider,
        List<IDisposable> owned)
    {
        MasterDatabase? master = null;
        RocksDbTimeSeriesStore? store = null;
        try
        {
            Directory.CreateDirectory(dataDirectory);
            master = MasterDatabase.Open(Path.Combine(dataDirectory, MasterFileName));
            store = RocksDbTimeSeriesStore.Open(Path.Combine(dataDirectory, TimeSeriesFolderName));
        }
        catch (Exception ex) when (ex is RocksDbException or SqliteException or IOException or UnauthorizedAccessException or NotSupportedException or DllNotFoundException)
        {
            // ほかのプロセスが開いている、ディスクに書けないなど。記録なしで動く。
            store?.Dispose();
            master?.Dispose();
            return (null, null);
        }

        owned.Add(master);
        owned.Add(store);

        var sensors = new SqliteSensorRepository(master);
        var retention = RetentionPolicy.Default;
        var persistence = new SensorPersistence(
            store,
            new TimeSeriesRecorder(store, new LogDecisionFilter()),
            new TimeSeriesRecorder(store, new LogDecisionFilter(), TimeSeriesResolution.OneSecond),
            new FineSeriesSelection(),
            new SensorCatalogSynchronizer(new SqliteHardwareRepository(master), sensors, new SqliteSourceRepository(master)),
            sensors,
            new SqliteEventLogRepository(master),
            new RollupJob(store, retention),
            new RetentionJob(store, retention),
            providerIds,
            timeProvider);
        return (persistence, master);
    }
}

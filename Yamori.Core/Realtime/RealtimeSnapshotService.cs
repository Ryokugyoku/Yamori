using Yamori.Core.Hardware;
using Yamori.Core.Hardware.DeviceIo;
using Yamori.Core.Hardware.Readers;
using Yamori.Core.Hardware.Wmi;
using Yamori.Core.Realtime.Collectors;
using Yamori.Core.Realtime.Counters;
using Yamori.Core.Realtime.Gpu;
using Yamori.Core.Realtime.Sources;

namespace Yamori.Core.Realtime;

/// <summary>
/// パフォーマンスカウンターをまとめて取り込み、部品ごとの集める処理を順に呼んで、今の状態を 1 つにまとめる。
/// </summary>
/// <remarks>スレッドセーフではない（1 つのスレッドから使うこと）。</remarks>
public sealed class RealtimeSnapshotService : IRealtimeSnapshotSource
{
    private static readonly TimeSpan DiskNameLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DiskReliabilityLifetime = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DiskTemperatureLifetime = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan NetworkAdapterLifetime = TimeSpan.FromSeconds(30);

    private readonly IPerformanceCounterSampler _sampler;
    private readonly ISystemPerformanceSource _system;
    private readonly IGpuTelemetrySource _gpuTelemetry;
    private readonly PowerMeterReader _meters;
    private readonly RealtimeCollectorSet _collectors;
    private readonly TimeProvider _timeProvider;
    private bool _hasBaseline;

    /// <summary>
    /// 使う部品を指定して作る。
    /// </summary>
    /// <param name="sampler">パフォーマンスカウンターの読み出し元。このインスタンスが破棄する。</param>
    /// <param name="system">Windows 全体のメモリなどの読み取り元。</param>
    /// <param name="gpuTelemetry">GPU のセンサーの値の読み取り元。</param>
    /// <param name="meters">電力量計の読み取り。</param>
    /// <param name="collectors">部品ごとの集める処理。</param>
    /// <param name="timeProvider">現在時刻の提供元。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public RealtimeSnapshotService(
        IPerformanceCounterSampler sampler,
        ISystemPerformanceSource system,
        IGpuTelemetrySource gpuTelemetry,
        PowerMeterReader meters,
        RealtimeCollectorSet collectors,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        ArgumentNullException.ThrowIfNull(system);
        ArgumentNullException.ThrowIfNull(gpuTelemetry);
        ArgumentNullException.ThrowIfNull(meters);
        ArgumentNullException.ThrowIfNull(collectors);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _sampler = sampler;
        _system = system;
        _gpuTelemetry = gpuTelemetry;
        _meters = meters;
        _collectors = collectors;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Windows の標準の情報源（PDH、D3DKMT、Win32 API、IOCTL、WMI）を使う構成で作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <param name="timeProvider">現在時刻の提供元。</param>
    /// <returns>新しいインスタンス。使い終わったら破棄すること。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public static RealtimeSnapshotService CreateDefault(IWmiQueryExecutor wmi, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var sampler = new PdhPerformanceCounterSampler();
        var diskNames = new PeriodicCache<IReadOnlyDictionary<int, string>>(
            new WmiDiskNameSource(wmi).ReadByDiskNumber, DiskNameLifetime, timeProvider);
        var wmiReliability = new PeriodicCache<IReadOnlyDictionary<int, StorageReliability>>(
            new StorageReliabilityReader(wmi).ReadByDiskNumber, DiskReliabilityLifetime, timeProvider);
        var resolver = new StorageReliabilityResolver(wmiReliability.Get, new IoctlStorageTemperatureSource());
        var reliability = new PeriodicCache<IReadOnlyDictionary<int, StorageReliability>>(
            () => resolver.Read(diskNames.Get().Keys), DiskTemperatureLifetime, timeProvider);
        var networkReader = new NetworkAdapterInfoReader(wmi);
        var networkAdapters = new PeriodicCache<IReadOnlyList<NetworkAdapterInfo>>(
            () => networkReader.ReadOrDefault([]), NetworkAdapterLifetime, timeProvider);

        return new RealtimeSnapshotService(
            sampler,
            new Win32SystemPerformanceSource(),
            new D3dkmtGpuTelemetrySource(),
            new PowerMeterReader(sampler),
            new RealtimeCollectorSet(
                Cpu: new CpuRealtimeCollector(sampler),
                Memory: new MemoryRealtimeCollector(sampler),
                Gpu: new GpuRealtimeCollector(sampler),
                Disks: new DiskRealtimeCollector(sampler, diskNames.Get, reliability.Get),
                Volumes: new VolumeRealtimeCollector(),
                Network: new NetworkRealtimeCollector(sampler, networkAdapters.Get),
                Power: new PowerRealtimeCollector(new Win32PowerStatusSource(), new WmiBatteryRateSource(wmi)),
                Cooling: new CoolingRealtimeCollector(sampler),
                Apps: new AppRealtimeCollector(sampler, Environment.ProcessorCount)),
            timeProvider);
    }

    /// <inheritdoc />
    public RealtimeSnapshot Take()
    {
        _sampler.Collect();

        var system = _system.Read();
        var meters = _meters.Read();
        var gpus = _collectors.Gpu.Collect(_gpuTelemetry.Read());

        var snapshot = new RealtimeSnapshot(
            Timestamp: _timeProvider.GetUtcNow(),
            Cpu: _collectors.Cpu.Collect(system, meters),
            Memory: _collectors.Memory.Collect(system, meters),
            Gpus: gpus,
            Storage: new StorageRealtime(_collectors.Disks.Collect(), _collectors.Volumes.Collect()),
            Networks: _collectors.Network.Collect(),
            Power: _collectors.Power.Collect(meters, gpus),
            Cooling: _collectors.Cooling.Collect(gpus),
            Apps: _collectors.Apps.Collect());

        if (!_hasBaseline)
        {
            // カウンターは最初の読み出しで取り込みの対象に加わる。ここで基準の値を取り込んでおくと、
            // 次の測定から 1 秒あたりの値まで分かる。
            _sampler.Collect();
            _hasBaseline = true;
        }

        return snapshot;
    }

    /// <summary>
    /// パフォーマンスカウンターのクエリを閉じる。
    /// </summary>
    public void Dispose() => _sampler.Dispose();
}

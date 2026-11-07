using Yamori.Core.Realtime.Collectors;

namespace Yamori.Core.Realtime;

/// <summary>
/// リアルタイム表示で使う、部品ごとの集める処理の組。
/// </summary>
/// <param name="Cpu">CPU の状態を集める処理。</param>
/// <param name="Memory">メモリの状態を集める処理。</param>
/// <param name="Gpu">GPU の状態を集める処理。</param>
/// <param name="Disks">物理ディスクの状態を集める処理。</param>
/// <param name="Volumes">ドライブの空き容量を集める処理。</param>
/// <param name="Network">ネットワーク アダプターの状態を集める処理。</param>
/// <param name="Power">電源の状態を集める処理。</param>
/// <param name="Cooling">ファンと温度センサーの状態を集める処理。</param>
/// <param name="Apps">アプリごとの使用量を集める処理。</param>
public sealed record RealtimeCollectorSet(
    CpuRealtimeCollector Cpu,
    MemoryRealtimeCollector Memory,
    GpuRealtimeCollector Gpu,
    DiskRealtimeCollector Disks,
    VolumeRealtimeCollector Volumes,
    NetworkRealtimeCollector Network,
    PowerRealtimeCollector Power,
    CoolingRealtimeCollector Cooling,
    AppRealtimeCollector Apps);

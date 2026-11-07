namespace Yamori.Core.Realtime;

/// <summary>
/// ある時点の、部品ごとの今の状態をまとめたもの。
/// </summary>
/// <param name="Timestamp">測った時刻（UTC）。</param>
/// <param name="Cpu">CPU の状態。</param>
/// <param name="Memory">メモリの状態。</param>
/// <param name="Gpus">GPU ごとの状態。</param>
/// <param name="Storage">ストレージの状態。</param>
/// <param name="Networks">表示するネットワーク アダプターごとの状態。</param>
/// <param name="Power">電源の状態。</param>
/// <param name="Cooling">ファンと温度センサーの状態。</param>
/// <param name="Apps">アプリごとの使用量。</param>
public sealed record RealtimeSnapshot(
    DateTimeOffset Timestamp,
    CpuRealtime Cpu,
    MemoryRealtime Memory,
    IReadOnlyList<GpuRealtime> Gpus,
    StorageRealtime Storage,
    IReadOnlyList<NetworkRealtime> Networks,
    PowerRealtime Power,
    CoolingRealtime Cooling,
    AppsRealtime Apps);

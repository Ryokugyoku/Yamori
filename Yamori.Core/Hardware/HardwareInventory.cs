namespace Yamori.Core.Hardware;

/// <summary>
/// PC のハードウェア一覧。取得できなかった部品は null または空の一覧になる。
/// </summary>
/// <param name="Cpu">CPU の情報。</param>
/// <param name="Memory">メモリの情報。</param>
/// <param name="Storage">ストレージの一覧。</param>
/// <param name="Gpus">GPU の一覧。</param>
/// <param name="Batteries">バッテリーの一覧。デスクトップ PC では空。</param>
public sealed record HardwareInventory(
    CpuInfo? Cpu,
    MemoryInfo? Memory,
    IReadOnlyList<StorageDeviceInfo> Storage,
    IReadOnlyList<GpuInfo> Gpus,
    IReadOnlyList<BatteryInfo> Batteries)
{
    /// <summary>
    /// 何も取得できなかったことを表す空の一覧。
    /// </summary>
    public static HardwareInventory Empty { get; } = new(null, null, [], [], []);
}

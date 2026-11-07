namespace Yamori.Core.Hardware;

/// <summary>
/// PC のハードウェア一覧。部品の種類（<see cref="HardwareComponent"/>）ごとに分類している。
/// 取得できなかった部品は null または空の一覧になる。
/// </summary>
/// <param name="Cpu">CPU の情報。内蔵 GPU は <see cref="CpuInfo.IntegratedGpu"/> に入る。</param>
/// <param name="Motherboard">マザーボードの情報。</param>
/// <param name="Memory">メモリの情報。</param>
/// <param name="Gpus">GPU の一覧。CPU の内蔵 GPU は含まない。</param>
/// <param name="Storage">ストレージの一覧。</param>
/// <param name="Fans">ファン（冷却装置）の一覧。</param>
/// <param name="PowerSupply">電源ユニット・バッテリー・電源の状態。</param>
/// <param name="NetworkAdapters">ネットワーク アダプターの一覧（仮想アダプターを含む）。</param>
public sealed record HardwareInventory(
    CpuInfo? Cpu,
    MotherboardInfo? Motherboard,
    MemoryInfo? Memory,
    IReadOnlyList<GpuInfo> Gpus,
    IReadOnlyList<StorageDeviceInfo> Storage,
    IReadOnlyList<FanInfo> Fans,
    PowerSupplyInfo PowerSupply,
    IReadOnlyList<NetworkAdapterInfo> NetworkAdapters)
{
    /// <summary>
    /// 何も取得できなかったことを表す空の一覧。
    /// </summary>
    public static HardwareInventory Empty { get; } = new(null, null, null, [], [], [], PowerSupplyInfo.Empty, []);
}

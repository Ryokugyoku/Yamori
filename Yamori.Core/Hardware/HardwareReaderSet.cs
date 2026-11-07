using Yamori.Core.Hardware.Readers;

namespace Yamori.Core.Hardware;

/// <summary>
/// 部品の種類（<see cref="HardwareComponent"/>）ごとの読み取り処理の組。
/// </summary>
/// <param name="Cpu">CPU の読み取り処理。</param>
/// <param name="Motherboard">マザーボードの読み取り処理。</param>
/// <param name="Memory">メモリの読み取り処理。</param>
/// <param name="Gpu">GPU の読み取り処理（内蔵 GPU を含めて読む）。</param>
/// <param name="Storage">ストレージの読み取り処理。</param>
/// <param name="Fan">ファンの読み取り処理。</param>
/// <param name="PowerSupply">電源まわりの読み取り処理。</param>
/// <param name="Network">ネットワーク アダプターの読み取り処理。</param>
public sealed record HardwareReaderSet(
    IHardwareReader<CpuInfo?> Cpu,
    IHardwareReader<MotherboardInfo?> Motherboard,
    IHardwareReader<MemoryInfo?> Memory,
    IHardwareReader<IReadOnlyList<GpuInfo>> Gpu,
    IHardwareReader<IReadOnlyList<StorageDeviceInfo>> Storage,
    IHardwareReader<IReadOnlyList<FanInfo>> Fan,
    IHardwareReader<PowerSupplyInfo> PowerSupply,
    IHardwareReader<IReadOnlyList<NetworkAdapterInfo>> Network);

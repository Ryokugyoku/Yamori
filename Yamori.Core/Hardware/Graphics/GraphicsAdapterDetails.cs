namespace Yamori.Core.Hardware.Graphics;

/// <summary>
/// DirectX（DXCore）から読み取った GPU 1 枚の詳しい情報。WMI の情報を補うために使う。
/// </summary>
/// <param name="Device">PCI のベンダー ID とデバイス ID。WMI の GPU と突き合わせるのに使う。</param>
/// <param name="DedicatedMemoryBytes">専用ビデオメモリ（バイト）。4GB を超えても正確。</param>
/// <param name="SharedMemoryBytes">共有メモリ（バイト）。</param>
/// <param name="IsIntegrated">CPU に内蔵された GPU かどうか。判別できない場合は null。</param>
public sealed record GraphicsAdapterDetails(
    PciDeviceId Device,
    ulong? DedicatedMemoryBytes,
    ulong? SharedMemoryBytes,
    bool? IsIntegrated);

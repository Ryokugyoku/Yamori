namespace Yamori.Core.Hardware;

/// <summary>
/// 物理ストレージ 1 台の情報。
/// </summary>
/// <param name="Model">製品名。</param>
/// <param name="SizeBytes">容量（バイト）。</param>
/// <param name="MediaType">ストレージの種類。</param>
/// <param name="BusType">接続方式（例：NVMe、SATA）。</param>
/// <param name="Health">Windows が報告する健康状態。</param>
public sealed record StorageDeviceInfo(
    string Model,
    ulong SizeBytes,
    StorageMediaType MediaType,
    string BusType,
    StorageHealth Health);

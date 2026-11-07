namespace Yamori.Core.Hardware;

/// <summary>
/// 物理ストレージ 1 台の情報。
/// </summary>
/// <param name="Model">製品名。</param>
/// <param name="SizeBytes">容量（バイト）。</param>
/// <param name="MediaType">ストレージの種類。</param>
/// <param name="BusType">接続方式（例：NVMe、SATA、USB）。</param>
/// <param name="Health">Windows が報告する健康状態。</param>
public sealed record StorageDeviceInfo(
    string Model,
    ulong SizeBytes,
    StorageMediaType MediaType,
    string BusType,
    StorageHealth Health)
{
    /// <summary>ファームウェアのバージョン。</summary>
    public string? FirmwareVersion { get; init; }

    /// <summary>HDD の回転数（rpm）。SSD や分からない場合は null。</summary>
    public int? SpindleSpeedRpm { get; init; }

    /// <summary>このストレージ上の、ドライブ文字のあるボリューム。</summary>
    public IReadOnlyList<StorageVolumeInfo> Volumes { get; init; } = [];

    /// <summary>温度・消耗度などの信頼性の情報。Windows が公開していない場合は null。</summary>
    public StorageReliability? Reliability { get; init; }
}

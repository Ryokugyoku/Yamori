using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// MSFT_PhysicalDisk から物理ストレージの一覧と健康状態を読み取る。管理者権限は不要。
/// </summary>
public sealed class StorageInfoReader : IHardwareReader<IReadOnlyList<StorageDeviceInfo>>
{
    private const string Query = "SELECT FriendlyName, Size, MediaType, BusType, HealthStatus FROM MSFT_PhysicalDisk";

    private static readonly IReadOnlyDictionary<int, string> BusTypeNames = new Dictionary<int, string>
    {
        [1] = "SCSI",
        [3] = "ATA",
        [7] = "USB",
        [8] = "RAID",
        [10] = "SAS",
        [11] = "SATA",
        [12] = "SD",
        [13] = "MMC",
        [15] = "File Backed Virtual",
        [16] = "Storage Spaces",
        [17] = "NVMe",
    };

    private readonly IWmiQueryExecutor _wmi;

    /// <summary>
    /// 読み取りに使う WMI 実行器を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public StorageInfoReader(IWmiQueryExecutor wmi)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        _wmi = wmi;
    }

    /// <inheritdoc />
    /// <returns>物理ストレージの一覧。</returns>
    public IReadOnlyList<StorageDeviceInfo> Read() =>
        _wmi.Query(WmiNamespaces.Storage, Query)
            .Select(r => new StorageDeviceInfo(
                Model: r.GetString("FriendlyName") ?? "不明なストレージ",
                SizeBytes: r.GetUInt64("Size") ?? 0,
                MediaType: ToMediaType(r.GetInt32("MediaType")),
                BusType: ToBusTypeName(r.GetInt32("BusType")),
                Health: ToHealth(r.GetInt32("HealthStatus"))))
            .ToArray();

    private static StorageMediaType ToMediaType(int? value) =>
        value is { } v && Enum.IsDefined(typeof(StorageMediaType), v) ? (StorageMediaType)v : StorageMediaType.Unknown;

    private static StorageHealth ToHealth(int? value) =>
        value is { } v && Enum.IsDefined(typeof(StorageHealth), v) ? (StorageHealth)v : StorageHealth.Unknown;

    private static string ToBusTypeName(int? value) =>
        value is { } v && BusTypeNames.TryGetValue(v, out var name) ? name : "不明";
}

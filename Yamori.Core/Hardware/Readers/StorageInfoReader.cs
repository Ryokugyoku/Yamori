using Yamori.Core.Hardware.DeviceIo;
using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// MSFT_PhysicalDisk から物理ストレージの一覧と健康状態を読み取り、ボリュームと信頼性の情報を添える。管理者権限は不要。
/// </summary>
public sealed class StorageInfoReader : IHardwareReader<IReadOnlyList<StorageDeviceInfo>>
{
    private const string Query =
        "SELECT DeviceId, FriendlyName, Size, MediaType, BusType, HealthStatus, FirmwareVersion, SpindleSpeed FROM MSFT_PhysicalDisk";

    /// <summary>SpindleSpeed が「分からない」を表す値。</summary>
    private const ulong UnknownSpindleSpeed = uint.MaxValue;

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
    private readonly StorageVolumeReader _volumeReader;
    private readonly StorageReliabilityResolver _reliabilityReader;

    /// <summary>
    /// 読み取りに使う WMI 実行器と、温度を直接問い合わせる情報源を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <param name="temperature">WMI で温度が取れないときに使う、温度の情報源。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public StorageInfoReader(IWmiQueryExecutor wmi, IStorageTemperatureSource temperature)
        : this(
            wmi,
            new StorageVolumeReader(wmi),
            new StorageReliabilityResolver(new StorageReliabilityReader(wmi).ReadByDiskNumber, temperature))
    {
    }

    /// <summary>
    /// 読み取りに使う処理を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <param name="volumeReader">ボリュームの読み取り処理。</param>
    /// <param name="reliabilityReader">信頼性の情報（温度を補ったもの）の読み取り処理。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public StorageInfoReader(IWmiQueryExecutor wmi, StorageVolumeReader volumeReader, StorageReliabilityResolver reliabilityReader)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        ArgumentNullException.ThrowIfNull(volumeReader);
        ArgumentNullException.ThrowIfNull(reliabilityReader);
        _wmi = wmi;
        _volumeReader = volumeReader;
        _reliabilityReader = reliabilityReader;
    }

    /// <inheritdoc />
    /// <returns>物理ストレージの一覧。</returns>
    public IReadOnlyList<StorageDeviceInfo> Read()
    {
        var disks = _wmi.Query(WmiNamespaces.Storage, Query);
        if (disks.Count == 0)
        {
            return [];
        }

        var volumes = _volumeReader.ReadByDiskNumber();
        var reliability = _reliabilityReader.Read(disks.Select(r => ParseDiskNumber(r)).OfType<int>());

        return disks
            .Select(r =>
            {
                var number = ParseDiskNumber(r);
                return new StorageDeviceInfo(
                    Model: r.GetString("FriendlyName") ?? "不明なストレージ",
                    SizeBytes: r.GetUInt64("Size") ?? 0,
                    MediaType: ToMediaType(r.GetInt32("MediaType")),
                    BusType: ToBusTypeName(r.GetInt32("BusType")),
                    Health: ToHealth(r.GetInt32("HealthStatus")))
                {
                    FirmwareVersion = OemPlaceholderFilter.Clean(r.GetString("FirmwareVersion")),
                    SpindleSpeedRpm = r.GetUInt64("SpindleSpeed") is > 0 and < UnknownSpindleSpeed and var rpm ? (int)rpm : null,
                    Volumes = number is { } v && volumes.TryGetValue(v, out var list) ? list : [],
                    Reliability = number is { } d && reliability.TryGetValue(d, out var counters) ? counters : null,
                };
            })
            .ToArray();
    }

    private static int? ParseDiskNumber(WmiRecord row) =>
        int.TryParse(row.GetString("DeviceId"), out var number) && number >= 0 ? number : null;

    private static StorageMediaType ToMediaType(int? value) =>
        value is { } v && Enum.IsDefined(typeof(StorageMediaType), v) ? (StorageMediaType)v : StorageMediaType.Unknown;

    private static StorageHealth ToHealth(int? value) =>
        value is { } v && Enum.IsDefined(typeof(StorageHealth), v) ? (StorageHealth)v : StorageHealth.Unknown;

    private static string ToBusTypeName(int? value) =>
        value is { } v && BusTypeNames.TryGetValue(v, out var name) ? name : "不明";
}

using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// MSFT_Partition と Win32_LogicalDisk から、物理ディスクごとのボリューム（ドライブ文字・空き容量）を読み取る。
/// </summary>
public sealed class StorageVolumeReader
{
    private const string PartitionQuery = "SELECT DiskNumber, DriveLetter FROM MSFT_Partition";
    private const string LogicalDiskQuery = "SELECT DeviceID, Size, FreeSpace, FileSystem FROM Win32_LogicalDisk";

    private readonly IWmiQueryExecutor _wmi;

    /// <summary>
    /// 読み取りに使う WMI 実行器を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public StorageVolumeReader(IWmiQueryExecutor wmi)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        _wmi = wmi;
    }

    /// <summary>
    /// 物理ディスクの番号ごとに、ボリュームの一覧を読み取る。どちらかのクラスが読めない場合は空になる。
    /// </summary>
    /// <returns>ディスクの番号（MSFT_PhysicalDisk.DeviceId）をキーにした、ボリュームの一覧。</returns>
    public IReadOnlyDictionary<int, IReadOnlyList<StorageVolumeInfo>> ReadByDiskNumber()
    {
        var logicalDisks = _wmi.QueryOrEmpty(WmiNamespaces.Cimv2, LogicalDiskQuery)
            .Select(r => (Letter: ToDriveLetter(r.GetString("DeviceID")), Row: r))
            .Where(x => x.Letter is not null)
            .GroupBy(x => x.Letter!.Value)
            .ToDictionary(g => g.Key, g => g.First().Row);

        return _wmi.QueryOrEmpty(WmiNamespaces.Storage, PartitionQuery)
            .Select(r => (Disk: r.GetInt32("DiskNumber"), Letter: ToDriveLetter(r.GetString("DriveLetter"))))
            .Where(x => x.Disk is not null && x.Letter is not null && logicalDisks.ContainsKey(x.Letter.Value))
            .GroupBy(x => x.Disk!.Value)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<StorageVolumeInfo>)g
                    .Select(x => ToVolume(x.Letter!.Value, logicalDisks[x.Letter.Value]))
                    .OrderBy(v => v.DriveLetter)
                    .ToArray());
    }

    private static StorageVolumeInfo ToVolume(char letter, WmiRecord logicalDisk) =>
        new(
            letter,
            logicalDisk.GetUInt64("Size") ?? 0,
            logicalDisk.GetUInt64("FreeSpace") ?? 0,
            logicalDisk.GetString("FileSystem"));

    private static char? ToDriveLetter(string? value) =>
        value is { Length: > 0 } && char.IsAsciiLetter(value[0]) ? char.ToUpperInvariant(value[0]) : null;
}

using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// MSFT_StorageReliabilityCounter から、物理ディスクごとの温度・消耗度・通電時間を読み取る。
/// </summary>
/// <remarks>
/// Windows はこのクラスを、管理者として実行したときだけ公開することが多い。読めない場合は空の結果にする（権限を求めることはしない）。
/// </remarks>
public sealed class StorageReliabilityReader
{
    private const string Query = "SELECT DeviceId, Temperature, Wear, PowerOnHours FROM MSFT_StorageReliabilityCounter";

    private readonly IWmiQueryExecutor _wmi;

    /// <summary>
    /// 読み取りに使う WMI 実行器を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public StorageReliabilityReader(IWmiQueryExecutor wmi)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        _wmi = wmi;
    }

    /// <summary>
    /// 物理ディスクの番号ごとに、信頼性の情報を読み取る。
    /// </summary>
    /// <returns>ディスクの番号（MSFT_PhysicalDisk.DeviceId）をキーにした信頼性の情報。読めない場合は空。</returns>
    public IReadOnlyDictionary<int, StorageReliability> ReadByDiskNumber() =>
        _wmi.QueryOrEmpty(WmiNamespaces.Storage, Query)
            .Select(r => (Disk: ParseDiskNumber(r.GetString("DeviceId")), Row: r))
            .Where(x => x.Disk is not null)
            .GroupBy(x => x.Disk!.Value)
            .ToDictionary(g => g.Key, g => ToReliability(g.First().Row));

    private static StorageReliability ToReliability(WmiRecord row) =>
        new(
            TemperatureCelsius: row.GetInt32("Temperature") is > 0 and var temperature ? temperature : null,
            WearPercent: row.GetInt32("Wear") is { } wear and <= 100 ? wear : null,
            PowerOnHours: row.GetInt32("PowerOnHours") is > 0 and var hours ? hours : null);

    private static int? ParseDiskNumber(string? deviceId) =>
        int.TryParse(deviceId, out var number) ? number : null;
}

using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Realtime.Sources;

/// <summary>
/// Win32_DiskDrive から、物理ディスクの番号ごとの識別子を読み取る。シリアル番号を使い、読めない場合は PnP デバイス ID を使う。
/// </summary>
/// <remarks>識別子はセンサーのキーを作るためだけに使い、そのままの形では画面や記録に出さない。</remarks>
public sealed class WmiDiskIdentitySource : IDiskIdentitySource
{
    private const string Query = "SELECT Index, SerialNumber, PNPDeviceID FROM Win32_DiskDrive";

    private readonly IWmiQueryExecutor _wmi;

    /// <summary>
    /// 読み取りに使う WMI 実行器を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public WmiDiskIdentitySource(IWmiQueryExecutor wmi)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        _wmi = wmi;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<int, string> ReadByDiskNumber() =>
        _wmi.QueryOrEmpty(WmiNamespaces.Cimv2, Query)
            .Select(r => (Index: r.GetInt32("Index"), Id: Identity(r.GetString("SerialNumber"), r.GetString("PNPDeviceID"))))
            .Where(x => x.Index is not null && x.Id is not null)
            .GroupBy(x => x.Index!.Value)
            .ToDictionary(g => g.Key, g => g.First().Id!);

    private static string? Identity(string? serial, string? pnpDeviceId) =>
        !string.IsNullOrWhiteSpace(serial) ? "serial:" + serial.Trim()
        : !string.IsNullOrWhiteSpace(pnpDeviceId) ? "pnp:" + pnpDeviceId.Trim()
        : null;
}

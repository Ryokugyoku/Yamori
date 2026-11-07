using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Realtime.Sources;

/// <summary>
/// Win32_DiskDrive から、物理ディスクの番号ごとの製品名を読み取る。
/// </summary>
public sealed class WmiDiskNameSource : IDiskNameSource
{
    private const string Query = "SELECT Index, Model FROM Win32_DiskDrive";

    private readonly IWmiQueryExecutor _wmi;

    /// <summary>
    /// 読み取りに使う WMI 実行器を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public WmiDiskNameSource(IWmiQueryExecutor wmi)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        _wmi = wmi;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<int, string> ReadByDiskNumber() =>
        _wmi.QueryOrEmpty(WmiNamespaces.Cimv2, Query)
            .Select(r => (Index: r.GetInt32("Index"), Model: r.GetString("Model")))
            .Where(x => x.Index is not null && x.Model is not null)
            .GroupBy(x => x.Index!.Value)
            .ToDictionary(g => g.Key, g => g.First().Model!);
}

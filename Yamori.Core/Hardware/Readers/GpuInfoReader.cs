using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// Win32_VideoController から GPU の一覧を読み取る。
/// </summary>
public sealed class GpuInfoReader : IHardwareReader<IReadOnlyList<GpuInfo>>
{
    private const string Query = "SELECT Name, AdapterRAM, DriverVersion FROM Win32_VideoController";

    private readonly IWmiQueryExecutor _wmi;

    /// <summary>
    /// 読み取りに使う WMI 実行器を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public GpuInfoReader(IWmiQueryExecutor wmi)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        _wmi = wmi;
    }

    /// <inheritdoc />
    /// <returns>GPU の一覧。</returns>
    public IReadOnlyList<GpuInfo> Read() =>
        _wmi.Query(WmiNamespaces.Cimv2, Query)
            .Select(r => new GpuInfo(
                Name: r.GetString("Name") ?? "不明な GPU",
                AdapterRamBytes: r.GetUInt64("AdapterRAM") is > 0 and var ram ? ram : null,
                DriverVersion: r.GetString("DriverVersion") ?? "不明"))
            .ToArray();
}

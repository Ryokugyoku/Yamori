using Yamori.Core.Hardware.Graphics;
using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// Win32_VideoController から GPU の一覧を読み取り、DXCore の情報（正確なビデオメモリ、内蔵かどうか）で補う。
/// </summary>
/// <remarks>
/// WMI と DXCore の GPU は、PCI のベンダー ID とデバイス ID で突き合わせる。同じ型番の GPU が複数ある場合は順番に割り当てる。
/// </remarks>
public sealed class GpuInfoReader : IHardwareReader<IReadOnlyList<GpuInfo>>
{
    private const string Query =
        "SELECT Name, AdapterCompatibility, AdapterRAM, DriverVersion, DriverDate, PNPDeviceID, CurrentHorizontalResolution, CurrentVerticalResolution, CurrentRefreshRate FROM Win32_VideoController";

    private readonly IWmiQueryExecutor _wmi;
    private readonly IGraphicsAdapterSource? _adapters;

    /// <summary>
    /// 読み取りに使う情報源を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <param name="adapters">GPU の詳しい情報の情報源。省略した場合は WMI の情報だけを使う。</param>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public GpuInfoReader(IWmiQueryExecutor wmi, IGraphicsAdapterSource? adapters = null)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        _wmi = wmi;
        _adapters = adapters;
    }

    /// <inheritdoc />
    /// <returns>GPU の一覧。内蔵 GPU も含む。</returns>
    public IReadOnlyList<GpuInfo> Read()
    {
        var rows = _wmi.Query(WmiNamespaces.Cimv2, Query);
        var unmatched = (_adapters?.GetAdapters() ?? []).ToList();
        return rows.Select(r => ToGpu(r, TakeMatch(unmatched, PciDeviceId.TryParse(r.GetString("PNPDeviceID"))))).ToArray();
    }

    private static GraphicsAdapterDetails? TakeMatch(List<GraphicsAdapterDetails> unmatched, PciDeviceId? device)
    {
        if (device is null)
        {
            return null;
        }

        var index = unmatched.FindIndex(a => a.Device == device.Value);
        if (index < 0)
        {
            return null;
        }

        var match = unmatched[index];
        unmatched.RemoveAt(index);
        return match;
    }

    private static GpuInfo ToGpu(WmiRecord row, GraphicsAdapterDetails? details)
    {
        // DXCore の値は 64 ビットで正確。WMI の AdapterRAM は 32 ビットのため、4GB を超えると切り詰められる。
        var wmiMemory = PositiveOrNull(row.GetUInt64("AdapterRAM"));
        var hasExactMemory = details?.DedicatedMemoryBytes is not null;

        return new GpuInfo(
            Name: row.GetString("Name") ?? "不明な GPU",
            DedicatedMemoryBytes: hasExactMemory ? PositiveOrNull(details!.DedicatedMemoryBytes) : wmiMemory,
            DriverVersion: row.GetString("DriverVersion") ?? "不明")
        {
            IsDedicatedMemoryCapped = !hasExactMemory && wmiMemory is not null,
            SharedMemoryBytes = PositiveOrNull(details?.SharedMemoryBytes),
            IsIntegrated = details?.IsIntegrated,
            Vendor = OemPlaceholderFilter.Clean(row.GetString("AdapterCompatibility")),
            DriverDate = row.GetDate("DriverDate"),
            HorizontalResolution = row.GetInt32("CurrentHorizontalResolution") is > 0 and var width ? width : null,
            VerticalResolution = row.GetInt32("CurrentVerticalResolution") is > 0 and var height ? height : null,
            RefreshRateHz = row.GetInt32("CurrentRefreshRate") is > 1 and var rate ? rate : null,
        };
    }

    private static ulong? PositiveOrNull(ulong? value) => value is > 0 ? value : null;
}

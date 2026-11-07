using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// MSFT_NetAdapter から、ネットワーク アダプターの一覧を読み取る。管理者権限は不要。
/// </summary>
/// <remarks>
/// MAC アドレスなどの個人を特定できる値は、クエリの対象にも含めない（読み取らない）。
/// </remarks>
public sealed class NetworkAdapterInfoReader : IHardwareReader<IReadOnlyList<NetworkAdapterInfo>>
{
    private const string Query =
        "SELECT InterfaceDescription, HardwareInterface, Virtual, NdisPhysicalMedium, InterfaceType, MediaConnectState, Speed, DriverVersionString FROM MSFT_NetAdapter";

    /// <summary>MediaConnectState の「つながっている」。</summary>
    private const int MediaConnected = 1;

    /// <summary>Speed が「分からない」を表す値。</summary>
    private const ulong UnknownSpeed = ulong.MaxValue;

    private readonly IWmiQueryExecutor _wmi;

    /// <summary>
    /// 読み取りに使う WMI 実行器を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public NetworkAdapterInfoReader(IWmiQueryExecutor wmi)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        _wmi = wmi;
    }

    /// <inheritdoc />
    /// <returns>ネットワーク アダプターの一覧。ハードウェアのアダプターを先に、名前の順に並べる。</returns>
    public IReadOnlyList<NetworkAdapterInfo> Read() =>
        _wmi.Query(WmiNamespaces.StandardCimv2, Query)
            .Select(ToInfo)
            .Where(a => a is not null)
            .Select(a => a!)
            .OrderByDescending(a => a.IsPhysical)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <summary>
    /// NDIS の物理メディアの種類と、インターフェースの種類（IANA の ifType）から接続方式を決める。
    /// </summary>
    /// <param name="ndisPhysicalMedium">NdisPhysicalMedium の値。</param>
    /// <param name="interfaceType">InterfaceType の値。</param>
    /// <returns>接続方式。</returns>
    public static NetworkConnectionKind ToConnectionKind(int? ndisPhysicalMedium, int? interfaceType) =>
        (ndisPhysicalMedium, interfaceType) switch
        {
            (1 or 9, _) => NetworkConnectionKind.WiFi,
            (8 or 12, _) => NetworkConnectionKind.Cellular,
            (10, _) => NetworkConnectionKind.Bluetooth,
            (14, _) => NetworkConnectionKind.Ethernet,
            (0 or null, 6) => NetworkConnectionKind.Ethernet,
            (0 or null, 71) => NetworkConnectionKind.WiFi,
            (0 or null, 243 or 244) => NetworkConnectionKind.Cellular,
            (null, null) => NetworkConnectionKind.Unknown,
            _ => NetworkConnectionKind.Other,
        };

    private static NetworkAdapterInfo? ToInfo(WmiRecord row)
    {
        var name = row.GetString("InterfaceDescription");
        if (name is null)
        {
            return null;
        }

        var isConnected = row.GetInt32("MediaConnectState") == MediaConnected;
        var isPhysical = row.GetBoolean("HardwareInterface") == true && row.GetBoolean("Virtual") != true;
        return new NetworkAdapterInfo(
            name,
            ToConnectionKind(row.GetInt32("NdisPhysicalMedium"), row.GetInt32("InterfaceType")),
            isPhysical,
            isConnected)
        {
            LinkSpeedBitsPerSecond = isConnected && row.GetUInt64("Speed") is > 0 and < UnknownSpeed and var speed ? speed : null,
            DriverVersion = row.GetString("DriverVersionString"),
        };
    }
}

using Yamori.Core.Hardware;

namespace Yamori.Core.Realtime;

/// <summary>
/// リアルタイム表示に出すネットワーク アダプターを決める。既定では、つながっているハードウェアのアダプターだけを出し、
/// 仮想アダプター（VPN、Hyper-V、WAN Miniport など）とループバックは出さない。
/// </summary>
public static class NetworkAdapterFilter
{
    /// <summary>
    /// アダプターの一覧が読めなかったときに、名前から仮想アダプターとみなす語。
    /// </summary>
    private static readonly string[] VirtualNameHints =
    [
        "Virtual",
        "Hyper-V",
        "vEthernet",
        "Loopback",
        "WAN Miniport",
        "Kernel Debug",
        "isatap",
        "Teredo",
        "6to4",
        "IP-HTTPS",
        "TAP-",
        "VPN",
        "Bluetooth",
        "Wi-Fi Direct",
    ];

    /// <summary>
    /// アダプターを表示するかどうかを決める。
    /// </summary>
    /// <param name="instanceName">パフォーマンスカウンターのインスタンス名。</param>
    /// <param name="adapter">インスタンスに対応するアダプターの情報。見つからなかった場合は null。</param>
    /// <param name="hasAdapterList">アダプターの一覧を読めたなら true。</param>
    /// <param name="linkSpeedBitsPerSecond">カウンターが報告するリンク速度（ビット／秒）。</param>
    /// <returns>表示するなら true。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="instanceName"/> が null。</exception>
    public static bool ShouldShow(string instanceName, NetworkAdapterInfo? adapter, bool hasAdapterList, double? linkSpeedBitsPerSecond)
    {
        ArgumentNullException.ThrowIfNull(instanceName);

        if (hasAdapterList)
        {
            // 一覧にない（隠れた仮想アダプターなど）か、仮想のもの、つながっていないものは出さない。
            return adapter is { IsPhysical: true, IsConnected: true };
        }

        // 一覧が読めない場合は、名前とリンク速度から判断する。
        return linkSpeedBitsPerSecond is > 0
            && !VirtualNameHints.Any(hint => instanceName.Contains(hint, StringComparison.OrdinalIgnoreCase));
    }
}

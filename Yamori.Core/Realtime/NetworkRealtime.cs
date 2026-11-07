using Yamori.Core.Hardware;

namespace Yamori.Core.Realtime;

/// <summary>
/// ネットワーク アダプター 1 つの今の状態。測れなかった値は null。
/// </summary>
/// <param name="Name">アダプターの製品名。</param>
/// <param name="ConnectionKind">接続方式。分からない場合は <see cref="NetworkConnectionKind.Unknown"/>。</param>
public sealed record NetworkRealtime(string Name, NetworkConnectionKind ConnectionKind)
{
    /// <summary>受信の速さ（バイト／秒）を取得する。</summary>
    public double? ReceivedBytesPerSecond { get; init; }

    /// <summary>送信の速さ（バイト／秒）を取得する。</summary>
    public double? SentBytesPerSecond { get; init; }

    /// <summary>今のリンク速度（ビット／秒）を取得する。</summary>
    public double? LinkSpeedBitsPerSecond { get; init; }

    /// <summary>使用率（%。送受信の合計をリンク速度で割った値）を取得する。</summary>
    public double? UsagePercent { get; init; }
}

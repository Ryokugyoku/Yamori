using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Yamori.Core.Sensors;

/// <summary>
/// <see cref="SensorKey.HardwareId"/> に使う、再起動しても変わらない ID を作る。列挙の順番は使わない。
/// </summary>
public static class HardwareIds
{
    /// <summary>
    /// システムに 1 つしかないもの（メインメモリ全体、Windows 全体、バッテリー全体の合計など）の ID。
    /// </summary>
    public const string Singleton = "0";

    private const int HashBytes = 4;

    /// <summary>
    /// PnP インスタンス ID、シリアル番号、PCI の位置など、永続的な識別子から短い ID を作る。
    /// SHA-256 の先頭 4 バイトを 16 進数 8 桁にしたもので、識別子そのもの（シリアル番号など）はキーに出さない。
    /// </summary>
    /// <param name="persistentId">永続的な識別子。前後の空白と大文字・小文字の違いは無視する。</param>
    /// <returns>小文字の 16 進数 8 桁の ID。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="persistentId"/> が null。</exception>
    /// <exception cref="ArgumentException"><paramref name="persistentId"/> が空か空白だけ。</exception>
    public static string FromPersistentId(string persistentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(persistentId);

        var bytes = Encoding.UTF8.GetBytes(persistentId.Trim().ToUpperInvariant());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash.AsSpan(0, HashBytes));
    }

    /// <summary>
    /// CPU のソケット（パッケージ）の番号から ID を作る。ACPI が割り当てる番号で、列挙の順番ではない。
    /// </summary>
    /// <param name="socket">ソケットの番号（0 から）。</param>
    /// <returns>番号をそのまま文字列にした ID（例：<c>0</c>）。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="socket"/> が負の値。</exception>
    public static string FromSocket(int socket)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(socket);
        return socket.ToString(CultureInfo.InvariantCulture);
    }
}

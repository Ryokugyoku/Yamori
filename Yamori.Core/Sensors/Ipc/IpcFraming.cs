using System.Buffers.Binary;

namespace Yamori.Core.Sensors.Ipc;

/// <summary>
/// パイプの上のメッセージの区切り方。先頭の 4 バイト（リトルエンディアン）に本文の長さを入れ、その後に本文（UTF-8 の JSON）を続ける。
/// </summary>
public static class IpcFraming
{
    /// <summary>1 つのメッセージの本文の最大の長さ（バイト）。これを超える長さは、壊れたデータとして扱う。</summary>
    public const int MaxFrameBytes = 16 * 1024 * 1024;

    private const int HeaderBytes = 4;

    /// <summary>
    /// メッセージを 1 つ書き込む。
    /// </summary>
    /// <param name="stream">書き込み先。</param>
    /// <param name="body">本文。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>書き込んだときに完了するタスク。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> が null。</exception>
    /// <exception cref="ArgumentException">本文が長すぎる。</exception>
    public static async Task WriteFrameAsync(Stream stream, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (body.Length > MaxFrameBytes)
        {
            throw new ArgumentException("メッセージが長すぎます。", nameof(body));
        }

        var buffer = new byte[HeaderBytes + body.Length];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, body.Length);
        body.CopyTo(buffer.AsMemory(HeaderBytes));
        await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// メッセージを 1 つ読む。
    /// </summary>
    /// <param name="stream">読み取り元。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>本文。相手が接続を閉じた場合は null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> が null。</exception>
    /// <exception cref="InvalidDataException">長さが不正、またはメッセージの途中で接続が閉じた。</exception>
    public static async Task<byte[]?> ReadFrameAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var header = new byte[HeaderBytes];
        var read = await ReadFullyAsync(stream, header, cancellationToken).ConfigureAwait(false);
        if (read == 0)
        {
            return null;
        }

        if (read < HeaderBytes)
        {
            throw new InvalidDataException("メッセージの途中で接続が閉じました。");
        }

        var length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length is < 0 or > MaxFrameBytes)
        {
            throw new InvalidDataException($"メッセージの長さが不正です: {length}");
        }

        var body = new byte[length];
        if (await ReadFullyAsync(stream, body, cancellationToken).ConfigureAwait(false) < length)
        {
            throw new InvalidDataException("メッセージの途中で接続が閉じました。");
        }

        return body;
    }

    private static async Task<int> ReadFullyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[total..], cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }
}

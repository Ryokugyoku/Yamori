using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Yamori.Core.Sensors.Ipc;

/// <summary>
/// パイプ 1 本の上で、<see cref="IpcEnvelope"/> を送り、受け取る。送るのは複数のスレッドから呼んでよく、受け取るのは 1 つのスレッドから呼ぶこと。
/// </summary>
public sealed class IpcConnection : IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    /// <summary>
    /// パイプを指定して作る。パイプは、このオブジェクトを破棄するときに一緒に閉じる。
    /// </summary>
    /// <param name="stream">接続済みのパイプ。</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> が null。</exception>
    public IpcConnection(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
    }

    /// <summary>
    /// 値を本文にする。
    /// </summary>
    /// <typeparam name="T">値の型。</typeparam>
    /// <param name="value">値。</param>
    /// <param name="typeInfo">値の型の JSON の変換（<see cref="SensorIpcJsonContext"/> のもの）。</param>
    /// <returns>本文。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="typeInfo"/> が null。</exception>
    public static JsonElement ToPayload<T>(T value, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        return JsonSerializer.SerializeToElement(value, typeInfo);
    }

    /// <summary>
    /// 本文を値に戻す。
    /// </summary>
    /// <typeparam name="T">値の型。</typeparam>
    /// <param name="payload">本文。</param>
    /// <param name="typeInfo">値の型の JSON の変換。</param>
    /// <returns>値。本文がない、または読めない場合は既定値。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="typeInfo"/> が null。</exception>
    public static T? FromPayload<T>(JsonElement? payload, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        if (payload is not { } element || element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return default;
        }

        try
        {
            return element.Deserialize(typeInfo);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// メッセージを 1 つ送る。
    /// </summary>
    /// <param name="envelope">メッセージ。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>送ったときに完了するタスク。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="envelope"/> が null。</exception>
    /// <exception cref="IOException">接続が切れた。</exception>
    public async Task SendAsync(IpcEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var body = JsonSerializer.SerializeToUtf8Bytes(envelope, SensorIpcJsonContext.Default.IpcEnvelope);

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await IpcFraming.WriteFrameAsync(_stream, body, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    /// <summary>
    /// メッセージを 1 つ受け取る。
    /// </summary>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>メッセージ。相手が接続を閉じた場合は null。</returns>
    /// <exception cref="InvalidDataException">メッセージが壊れている。</exception>
    /// <exception cref="IOException">接続が切れた。</exception>
    public async Task<IpcEnvelope?> ReceiveAsync(CancellationToken cancellationToken)
    {
        var body = await IpcFraming.ReadFrameAsync(_stream, cancellationToken).ConfigureAwait(false);
        if (body is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(body, SensorIpcJsonContext.Default.IpcEnvelope)
                ?? throw new InvalidDataException("メッセージが空です。");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("メッセージを読めません。", ex);
        }
    }

    /// <summary>
    /// パイプを閉じる。
    /// </summary>
    /// <returns>閉じたときに完了するタスク。</returns>
    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync().ConfigureAwait(false);
        _writeGate.Dispose();
    }
}

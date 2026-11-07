using System.IO.Pipes;
using System.Security.Principal;

namespace Yamori.Core.Sensors.Ipc.Client;

/// <summary>
/// Helper のパイプにつなぐ処理を作る。
/// </summary>
public static class SensorPipeConnector
{
    /// <summary>
    /// 名前付きパイプにつなぐ処理を作る。相手には「誰か」を確かめることだけを許し（Identification）、自分として動くことは許さない。
    /// </summary>
    /// <param name="pipeName">パイプの名前。</param>
    /// <param name="connectTimeout">つながるまで待つ最長の時間。</param>
    /// <returns>つないだパイプを返す処理。つながらなければ <see cref="TimeoutException"/> か <see cref="IOException"/> を投げる。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pipeName"/> が null。</exception>
    public static Func<CancellationToken, Task<Stream>> Create(string pipeName, TimeSpan connectTimeout)
    {
        ArgumentNullException.ThrowIfNull(pipeName);
        return async cancellationToken =>
        {
            var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Identification);
            try
            {
                await pipe.ConnectAsync(connectTimeout, cancellationToken).ConfigureAwait(false);
                return pipe;
            }
            catch
            {
                await pipe.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        };
    }
}

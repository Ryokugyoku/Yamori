using System.Collections.Concurrent;
using System.IO.Pipes;
using Yamori.Core.AppUsage.Service;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Ipc.Messages;
using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Sensors.Ipc.Server;

/// <summary>
/// Helper の中で動く、センサーの名前付きパイプのサーバー。複数のクライアント（サインインしている複数のユーザーの GUI）が同時につなげる。
/// 最新値・カタログ・拡張センサーの状態の変化は、つないでいるすべてのクライアントに通知する。
/// </summary>
public sealed class SensorPipeServer
{
    private static readonly TimeSpan BusyRetryDelay = TimeSpan.FromMilliseconds(500);

    private readonly ISensorService _service;
    private readonly SensorPipeServerOptions _options;
    private readonly SensorRequestHandler _handler;
    private readonly ConcurrentDictionary<SensorPipeSession, Task> _sessions = new();

    /// <summary>
    /// 依頼を渡す先と設定を指定して作る。
    /// </summary>
    /// <param name="service">センサーの窓口（Helper の中の <see cref="LocalSensorService"/>）。</param>
    /// <param name="options">設定。</param>
    /// <param name="appUsage">アプリごとの使用率の記録の窓口。null なら常に記録なしを返す。</param>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> か <paramref name="options"/> が null。</exception>
    public SensorPipeServer(ISensorService service, SensorPipeServerOptions options, IAppUsageService? appUsage = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(options);
        _service = service;
        _options = options;
        _handler = new SensorRequestHandler(service, appUsage);
    }

    /// <summary>今つないでいるクライアントの数を取得する。</summary>
    public int ConnectionCount => _sessions.Count;

    /// <summary>
    /// 止める合図が来るまで、接続を受け付け続ける。
    /// </summary>
    /// <param name="cancellationToken">止める合図。</param>
    /// <returns>すべての接続を閉じたときに完了するタスク。止める合図による停止では例外を投げない。</returns>
    /// <exception cref="UnauthorizedAccessException">同じ名前のパイプをほかのプロセスが先に作っている。</exception>
    /// <exception cref="IOException">同じ名前のパイプをほかのプロセスが先に作っている。</exception>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _service.ReadingsUpdated += OnReadingsUpdated;
        _service.CatalogChanged += OnCatalogChanged;
        _service.ExtendedSensorStateChanged += OnExtendedStateChanged;
        try
        {
            var first = true;
            while (!cancellationToken.IsCancellationRequested)
            {
                NamedPipeServerStream pipe;
                try
                {
                    pipe = CreatePipe(first);
                }
                catch (IOException) when (!first)
                {
                    // 最大の接続数に達している。少し待ってから受け付け直す。
                    await Task.Delay(BusyRetryDelay, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                    continue;
                }

                first = false;
                try
                {
                    await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    await pipe.DisposeAsync().ConfigureAwait(false);
                    break;
                }
                catch (IOException)
                {
                    // 受け付けの途中で相手が切った。
                    await pipe.DisposeAsync().ConfigureAwait(false);
                    continue;
                }

                StartSession(pipe, cancellationToken);
            }
        }
        finally
        {
            _service.ReadingsUpdated -= OnReadingsUpdated;
            _service.CatalogChanged -= OnCatalogChanged;
            _service.ExtendedSensorStateChanged -= OnExtendedStateChanged;
            await Task.WhenAll(_sessions.Values).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    private void StartSession(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        var session = new SensorPipeSession(pipe, _handler, _options);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var run = Task.Run(async () =>
        {
            await started.Task.ConfigureAwait(false);
            try
            {
                await session.RunAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _sessions.TryRemove(session, out _);
            }
        });
        _sessions[session] = run;
        started.SetResult();
    }

    private NamedPipeServerStream CreatePipe(bool first)
    {
        // 最初の 1 本は「最初のインスタンス」として作り、ほかのプロセスが先に同じ名前で待ち受けていたら失敗させる。
        var options = PipeOptions.Asynchronous | (first ? PipeOptions.FirstPipeInstance : PipeOptions.None);
        return _options.Security is null
            ? new NamedPipeServerStream(_options.PipeName, PipeDirection.InOut, _options.MaxConnections, PipeTransmissionMode.Byte, options)
            : NamedPipeServerStreamAcl.Create(
                _options.PipeName,
                PipeDirection.InOut,
                _options.MaxConnections,
                PipeTransmissionMode.Byte,
                options,
                inBufferSize: 0,
                outBufferSize: 0,
                _options.Security);
    }

    private void Broadcast(SensorNotificationKind kind, System.Text.Json.JsonElement payload)
    {
        if (_sessions.IsEmpty)
        {
            return;
        }

        var envelope = new IpcEnvelope { Kind = IpcMessageKind.Notification, Notification = kind, Payload = payload };
        foreach (var session in _sessions.Keys)
        {
            session.Enqueue(envelope);
        }
    }

    private void OnReadingsUpdated(object? sender, IReadOnlyList<SensorReading> readings)
    {
        if (!_sessions.IsEmpty)
        {
            Broadcast(
                SensorNotificationKind.ReadingsUpdated,
                IpcConnection.ToPayload(readings.Select(SensorDtoMapper.ToDto).ToArray(), SensorIpcJsonContext.Default.SensorReadingDtoArray));
        }
    }

    private void OnCatalogChanged(object? sender, IReadOnlyList<SensorDescriptor> catalog) =>
        Broadcast(
            SensorNotificationKind.CatalogChanged,
            IpcConnection.ToPayload(catalog.Select(SensorDtoMapper.ToDto).ToArray(), SensorIpcJsonContext.Default.SensorDescriptorDtoArray));

    private void OnExtendedStateChanged(object? sender, ExtendedSensorState state) =>
        Broadcast(
            SensorNotificationKind.ExtendedSensorStateChanged,
            IpcConnection.ToPayload(new ExtendedStateMessage(state), SensorIpcJsonContext.Default.ExtendedStateMessage));
}

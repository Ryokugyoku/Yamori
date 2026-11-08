using System.Text.Json;
using Yamori.Core.Anomalies;
using Yamori.Core.Anomalies.Service;
using Yamori.Core.AppUsage.Analysis;
using Yamori.Core.AppUsage.Service;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Ipc.Messages;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Sensors.Ipc.Client;

/// <summary>
/// Helper にパイプでつなぐ <see cref="ISensorService"/>。GUI はストアのファイルを開かず、すべてこれを通して扱う。
/// 切れたら自動でつなぎ直す（<see cref="RunAsync"/> を呼んでいる間）。
/// </summary>
/// <remarks>
/// つながっていない間の操作は例外を投げず、空の結果（最新値は <see cref="SensorStatus.TemporarilyUnavailable"/>、設定の変更は
/// <see cref="SensorSettingResult.Unavailable"/>）を返す。
/// </remarks>
public sealed class RemoteSensorService : ISensorService, IHelperConnection, IAppUsageService, IAnomalyService
{
    private readonly Func<CancellationToken, Task<Stream>> _connect;
    private readonly TimeProvider _timeProvider;
    private readonly RemoteSensorServiceOptions _options;
    private volatile SensorPipeClient? _client;
    private int _state = (int)HelperConnectionState.Connecting;

    /// <summary>
    /// つなぐ処理を指定して作る。
    /// </summary>
    /// <param name="connect">Helper のパイプにつなぐ処理（<see cref="SensorPipeConnector.Create"/>）。</param>
    /// <param name="timeProvider">現在時刻とタイマーの提供元。</param>
    /// <param name="options">待ち時間の設定。null なら既定の設定。</param>
    /// <exception cref="ArgumentNullException"><paramref name="connect"/> か <paramref name="timeProvider"/> が null。</exception>
    public RemoteSensorService(Func<CancellationToken, Task<Stream>> connect, TimeProvider timeProvider, RemoteSensorServiceOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(connect);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _connect = connect;
        _timeProvider = timeProvider;
        _options = options ?? new RemoteSensorServiceOptions();
    }

    /// <inheritdoc />
    public event EventHandler<IReadOnlyList<SensorReading>>? ReadingsUpdated;

    /// <inheritdoc />
    public event EventHandler<IReadOnlyList<SensorDescriptor>>? CatalogChanged;

    /// <inheritdoc />
    public event EventHandler<ExtendedSensorState>? ExtendedSensorStateChanged;

    /// <inheritdoc />
    public event EventHandler<HelperConnectionState>? ConnectionStateChanged;

    /// <inheritdoc />
    public event EventHandler<AnomalyNotice>? AnomalyChanged;

    /// <inheritdoc />
    public int ContractVersion => SensorContract.Version;

    /// <inheritdoc />
    public HelperConnectionState ConnectionState => (HelperConnectionState)Volatile.Read(ref _state);

    /// <summary>
    /// 止める合図が来るまで、Helper につなぎ、切れたらつなぎ直す。つないだら、カタログと拡張センサーの状態を取り直して通知する。
    /// </summary>
    /// <param name="cancellationToken">止める合図。</param>
    /// <returns>止まったときに完了するタスク。止める合図による停止では例外を投げない。</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            SensorPipeClient client;
            try
            {
                client = await ConnectAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (SensorContractMismatchException)
            {
                SetState(HelperConnectionState.Incompatible);
                await DelayAsync(_options.IncompatibleRetryDelay, cancellationToken).ConfigureAwait(false);
                continue;
            }
            catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException or OperationCanceledException)
            {
                SetState(HelperConnectionState.NotRunning);
                await DelayAsync(_options.RetryDelay, cancellationToken).ConfigureAwait(false);
                continue;
            }

            await RunConnectedAsync(client, cancellationToken).ConfigureAwait(false);
            if (!cancellationToken.IsCancellationRequested)
            {
                SetState(HelperConnectionState.NotRunning);
                await DelayAsync(_options.RetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SensorDescriptor>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var response = await TryRequestAsync(SensorOperation.GetCatalog, null, cancellationToken).ConfigureAwait(false);
        return ToCatalog(IpcConnection.FromPayload(response?.Payload, SensorIpcJsonContext.Default.SensorDescriptorDtoArray));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SensorReading>> GetLatestAsync(IReadOnlyCollection<SensorKey> keys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var payload = IpcConnection.ToPayload(new KeysRequest(keys.Select(k => k.ToString()).ToArray()), SensorIpcJsonContext.Default.KeysRequest);
        var response = await TryRequestAsync(SensorOperation.GetLatest, payload, cancellationToken).ConfigureAwait(false);
        var readings = IpcConnection.FromPayload(response?.Payload, SensorIpcJsonContext.Default.SensorReadingDtoArray);
        if (readings is null)
        {
            var now = _timeProvider.GetUtcNow();
            return keys.Select(k => SensorReading.Failed(k, SensorStatus.TemporarilyUnavailable, now, string.Empty)).ToArray();
        }

        return ToReadings(readings);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SensorReading>> GetRecentAsync(SensorKey key, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var payload = IpcConnection.ToPayload(new RecentRequest(key.ToString(), (long)window.TotalMilliseconds), SensorIpcJsonContext.Default.RecentRequest);
        var response = await TryRequestAsync(SensorOperation.GetRecent, payload, cancellationToken).ConfigureAwait(false);
        return ToReadings(IpcConnection.FromPayload(response?.Payload, SensorIpcJsonContext.Default.SensorReadingDtoArray) ?? []);
    }

    /// <inheritdoc />
    /// <remarks>Helper につながっていない場合は <see cref="ExtendedSensorState.HelperRequired"/>。</remarks>
    public async Task<ExtendedSensorState> GetExtendedSensorStateAsync(CancellationToken cancellationToken = default)
    {
        var response = await TryRequestAsync(SensorOperation.GetExtendedSensorState, null, cancellationToken).ConfigureAwait(false);
        return response is null
            ? ExtendedSensorState.HelperRequired
            : IpcConnection.FromPayload(response.Payload, SensorIpcJsonContext.Default.ExtendedStateMessage)?.State ?? ExtendedSensorState.NotInstalled;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SensorHistoryPoint>> GetHistoryAsync(
        SensorKey key,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        TimeSeriesResolution resolution,
        CancellationToken cancellationToken = default)
    {
        var payload = IpcConnection.ToPayload(new HistoryRequest(key.ToString(), fromUtc, toUtc, resolution), SensorIpcJsonContext.Default.HistoryRequest);
        var response = await TryRequestAsync(SensorOperation.GetHistory, payload, cancellationToken).ConfigureAwait(false);
        var points = IpcConnection.FromPayload(response?.Payload, SensorIpcJsonContext.Default.SensorHistoryPointDtoArray) ?? [];
        return points.Where(p => p is not null).Select(SensorDtoMapper.FromDto).ToArray();
    }

    /// <inheritdoc />
    public async Task<SensorLogPolicy?> GetLogPolicyAsync(SensorKey key, CancellationToken cancellationToken = default)
    {
        var payload = IpcConnection.ToPayload(new KeyRequest(key.ToString()), SensorIpcJsonContext.Default.KeyRequest);
        var response = await TryRequestAsync(SensorOperation.GetLogPolicy, payload, cancellationToken).ConfigureAwait(false);
        var dto = IpcConnection.FromPayload(response?.Payload, SensorIpcJsonContext.Default.LogPolicyResponse)?.Policy;
        return dto is null ? null : SensorDtoMapper.FromDto(dto);
    }

    /// <inheritdoc />
    public async Task<SensorSettingResult> SetLogPolicyAsync(SensorKey key, SensorLogPolicy policy, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var payload = IpcConnection.ToPayload(new SetLogPolicyRequest(key.ToString(), SensorDtoMapper.ToDto(policy)), SensorIpcJsonContext.Default.SetLogPolicyRequest);
        var response = await TryRequestAsync(SensorOperation.SetLogPolicy, payload, cancellationToken).ConfigureAwait(false);
        return ToSettingResult(response);
    }

    /// <inheritdoc />
    public async Task<bool> GetPawnIoConsentAsync(CancellationToken cancellationToken = default)
    {
        var response = await TryRequestAsync(SensorOperation.GetPawnIoConsent, null, cancellationToken).ConfigureAwait(false);
        return IpcConnection.FromPayload(response?.Payload, SensorIpcJsonContext.Default.ConsentMessage)?.Enabled ?? false;
    }

    /// <inheritdoc />
    public async Task<SensorSettingResult> SetPawnIoConsentAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        var payload = IpcConnection.ToPayload(new ConsentMessage(enabled), SensorIpcJsonContext.Default.ConsentMessage);
        var response = await TryRequestAsync(SensorOperation.SetPawnIoConsent, payload, cancellationToken).ConfigureAwait(false);
        return ToSettingResult(response);
    }

    /// <inheritdoc />
    /// <remarks>Helper につながっていない場合や、Helper が古くこの操作を知らない場合は <see cref="AppCpuUsageSummary.Empty"/>。</remarks>
    public async Task<AppCpuUsageSummary> GetCpuUsageAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        double highLoadThresholdPercent,
        int top,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var payload = IpcConnection.ToPayload(
            new AppCpuUsageRequest(fromUtc, toUtc, highLoadThresholdPercent, top),
            SensorIpcJsonContext.Default.AppCpuUsageRequest);
        var response = await TryRequestAsync(SensorOperation.GetAppCpuUsage, payload, cancellationToken).ConfigureAwait(false);
        return AppUsageDtoMapper.FromDto(IpcConnection.FromPayload(response?.Payload, SensorIpcJsonContext.Default.AppCpuUsageResponse));
    }

    /// <inheritdoc />
    /// <remarks>Helper につながっていない場合や、Helper が古くこの操作を知らない場合は空。</remarks>
    public async Task<IReadOnlyList<AnomalyRecord>> GetAnomaliesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        bool includeDismissed,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(limit);
        if (limit == 0 || toUtc <= fromUtc)
        {
            return [];
        }

        var payload = IpcConnection.ToPayload(
            new AnomaliesRequest(fromUtc, toUtc, includeDismissed, Math.Min(limit, LocalAnomalyService.MaxLimit)),
            SensorIpcJsonContext.Default.AnomaliesRequest);
        var response = await TryRequestAsync(SensorOperation.GetAnomalies, payload, cancellationToken).ConfigureAwait(false);
        var dtos = IpcConnection.FromPayload(response?.Payload, SensorIpcJsonContext.Default.AnomalyDtoArray) ?? [];
        return dtos.Where(d => d is not null).Select(AnomalyDtoMapper.FromDto).OfType<AnomalyRecord>().ToArray();
    }

    /// <inheritdoc />
    public async Task<SensorSettingResult> SetDismissedAsync(long anomalyId, bool dismissed, CancellationToken cancellationToken = default)
    {
        var payload = IpcConnection.ToPayload(new AnomalyDismissRequest(anomalyId, dismissed), SensorIpcJsonContext.Default.AnomalyDismissRequest);
        var response = await TryRequestAsync(SensorOperation.SetAnomalyDismissed, payload, cancellationToken).ConfigureAwait(false);
        return ToSettingResult(response);
    }

    /// <inheritdoc />
    public async Task<SensorSettingResult> MarkNotifiedAsync(long anomalyId, CancellationToken cancellationToken = default)
    {
        var payload = IpcConnection.ToPayload(new AnomalyIdRequest(anomalyId), SensorIpcJsonContext.Default.AnomalyIdRequest);
        var response = await TryRequestAsync(SensorOperation.MarkAnomalyNotified, payload, cancellationToken).ConfigureAwait(false);
        return ToSettingResult(response);
    }

    /// <inheritdoc />
    /// <remarks>Helper につながっていない場合や、Helper が古くこの操作を知らない場合は空。</remarks>
    public async Task<IReadOnlyList<AnomalySetting>> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var response = await TryRequestAsync(SensorOperation.GetAnomalySettings, null, cancellationToken).ConfigureAwait(false);
        var dtos = IpcConnection.FromPayload(response?.Payload, SensorIpcJsonContext.Default.AnomalySettingDtoArray) ?? [];
        return dtos.Where(d => d is not null).Select(AnomalyDtoMapper.FromDto).OfType<AnomalySetting>().ToArray();
    }

    /// <inheritdoc />
    public async Task<SensorSettingResult> SetSettingAsync(AnomalySetting setting, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(setting);

        var payload = IpcConnection.ToPayload(AnomalyDtoMapper.ToDto(setting), SensorIpcJsonContext.Default.AnomalySettingDto);
        var response = await TryRequestAsync(SensorOperation.SetAnomalySetting, payload, cancellationToken).ConfigureAwait(false);
        return ToSettingResult(response);
    }

    /// <inheritdoc />
    public async Task<bool?> GetNotificationsEnabledAsync(CancellationToken cancellationToken = default)
    {
        var response = await TryRequestAsync(SensorOperation.GetAnomalyNotificationsEnabled, null, cancellationToken).ConfigureAwait(false);
        return IpcConnection.FromPayload(response?.Payload, SensorIpcJsonContext.Default.AnomalyNotificationsMessage)?.Enabled;
    }

    /// <inheritdoc />
    public async Task<SensorSettingResult> SetNotificationsEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        var payload = IpcConnection.ToPayload(new AnomalyNotificationsMessage(enabled), SensorIpcJsonContext.Default.AnomalyNotificationsMessage);
        var response = await TryRequestAsync(SensorOperation.SetAnomalyNotificationsEnabled, payload, cancellationToken).ConfigureAwait(false);
        return ToSettingResult(response);
    }

    private static SensorSettingResult ToSettingResult(IpcEnvelope? response) =>
        IpcConnection.FromPayload(response?.Payload, SensorIpcJsonContext.Default.SettingResultResponse)?.Result ?? SensorSettingResult.Unavailable;

    private static IReadOnlyList<SensorDescriptor> ToCatalog(SensorDescriptorDto[]? dtos) =>
        (dtos ?? []).Where(d => d is not null).Select(SensorDtoMapper.FromDto).OfType<SensorDescriptor>().ToArray();

    private static IReadOnlyList<SensorReading> ToReadings(SensorReadingDto[] dtos) =>
        dtos.Where(d => d is not null).Select(SensorDtoMapper.FromDto).OfType<SensorReading>().ToArray();

    private async Task<SensorPipeClient> ConnectAsync(CancellationToken cancellationToken)
    {
        var stream = await _connect(cancellationToken).ConfigureAwait(false);
        return await SensorPipeClient.ConnectAsync(stream, _options.HandshakeTimeout, cancellationToken, _options.Topics).ConfigureAwait(false);
    }

    private async Task RunConnectedAsync(SensorPipeClient client, CancellationToken cancellationToken)
    {
        client.NotificationReceived += OnNotification;
        client.Start();
        _client = client;
        SetState(HelperConnectionState.Connected);
        try
        {
            // つないだ直後に今の状態をそろえる（切れていた間の変化を取りこぼさないため）。
            if (_options.Topics.Includes(SensorNotificationKind.CatalogChanged))
            {
                var catalog = await GetCatalogAsync(cancellationToken).ConfigureAwait(false);
                CatalogChanged?.Invoke(this, catalog);
            }

            if (_options.Topics.Includes(SensorNotificationKind.ExtendedSensorStateChanged))
            {
                var state = await GetExtendedSensorStateAsync(cancellationToken).ConfigureAwait(false);
                ExtendedSensorStateChanged?.Invoke(this, state);
            }

            await client.Completion.WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
        finally
        {
            _client = null;
            client.NotificationReceived -= OnNotification;
            await client.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<IpcEnvelope?> TryRequestAsync(SensorOperation operation, JsonElement? payload, CancellationToken cancellationToken)
    {
        if (_client is not { } client)
        {
            return null;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.RequestTimeout);
        try
        {
            var response = await client.RequestAsync(operation, payload, timeout.Token).ConfigureAwait(false);
            return response.Status == IpcResponseStatus.Ok ? response : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // 返事が来なかった。
            return null;
        }
    }

    private void OnNotification(object? sender, IpcEnvelope notification)
    {
        if (!_options.Topics.Includes(notification.Notification))
        {
            // 選んでいない種類（選べない版 2 までの Helper が送ってきたもの）は流さない。
            return;
        }

        switch (notification.Notification)
        {
            case SensorNotificationKind.ReadingsUpdated:
                var readings = IpcConnection.FromPayload(notification.Payload, SensorIpcJsonContext.Default.SensorReadingDtoArray);
                if (readings is not null)
                {
                    ReadingsUpdated?.Invoke(this, ToReadings(readings));
                }

                break;
            case SensorNotificationKind.CatalogChanged:
                var catalog = IpcConnection.FromPayload(notification.Payload, SensorIpcJsonContext.Default.SensorDescriptorDtoArray);
                if (catalog is not null)
                {
                    CatalogChanged?.Invoke(this, ToCatalog(catalog));
                }

                break;
            case SensorNotificationKind.ExtendedSensorStateChanged:
                if (IpcConnection.FromPayload(notification.Payload, SensorIpcJsonContext.Default.ExtendedStateMessage) is { } state)
                {
                    ExtendedSensorStateChanged?.Invoke(this, state.State);
                }

                break;
            case SensorNotificationKind.AnomalyChanged:
                if (IpcConnection.FromPayload(notification.Payload, SensorIpcJsonContext.Default.AnomalyNoticeDto) is { } dto
                    && AnomalyDtoMapper.FromDto(dto) is { } notice)
                {
                    AnomalyChanged?.Invoke(this, notice);
                }

                break;
            default:
                // 知らない種類の通知は無視する（新しい版の Helper）。
                break;
        }
    }

    private void SetState(HelperConnectionState state)
    {
        if (Interlocked.Exchange(ref _state, (int)state) != (int)state)
        {
            ConnectionStateChanged?.Invoke(this, state);
        }
    }

    private async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
}

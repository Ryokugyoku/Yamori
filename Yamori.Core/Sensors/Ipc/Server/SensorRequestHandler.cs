using System.Text.Json;
using Yamori.Core.Anomalies.Service;
using Yamori.Core.AppUsage.Service;
using Yamori.Core.Sensors.Ipc.Messages;
using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Sensors.Ipc.Server;

/// <summary>
/// パイプで受けた依頼を、決まった操作（<see cref="SensorOperation"/>）だけ <see cref="ISensorService"/> などに渡して返事を作る。
/// 設定の変更のうち、記録の設定と PawnIO の同意は管理者からの依頼だけを受け付ける。異常の設定と「これは正常」は、
/// 安全に関わらないため管理者でなくても受け付ける。
/// </summary>
public sealed class SensorRequestHandler
{
    private static readonly TimeSpan MaxRecentWindow = TimeSpan.FromDays(1);

    /// <summary>アプリごとの使用率のまとめで、1 回に頼める最長の期間。</summary>
    private static readonly TimeSpan MaxAppUsageRange = TimeSpan.FromDays(100);

    /// <summary>異常の一覧で、1 回に頼める最長の期間。</summary>
    private static readonly TimeSpan MaxAnomalyRange = TimeSpan.FromDays(400);

    /// <summary>アプリごとの使用率のまとめで、1 回に返せる上位の数の上限。</summary>
    private const int MaxAppUsageTop = 50;

    private readonly ISensorService _service;
    private readonly IAppUsageService _appUsage;
    private readonly IAnomalyService _anomalies;

    /// <summary>
    /// 依頼を渡す先を指定して作る。
    /// </summary>
    /// <param name="service">センサーの窓口（Helper の中の <see cref="LocalSensorService"/>）。</param>
    /// <param name="appUsage">アプリごとの使用率の記録の窓口。null なら常に記録なしを返す。</param>
    /// <param name="anomalies">異常の一覧と設定の窓口。null なら一覧は空、変更は <see cref="SensorSettingResult.Unavailable"/> を返す。</param>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> が null。</exception>
    public SensorRequestHandler(ISensorService service, IAppUsageService? appUsage = null, IAnomalyService? anomalies = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        _service = service;
        _appUsage = appUsage ?? new LocalAppUsageService(null);
        _anomalies = anomalies ?? new LocalAnomalyService(null, null, null, TimeProvider.System);
    }

    /// <summary>
    /// 依頼を処理して返事を作る。
    /// </summary>
    /// <param name="request">依頼（<see cref="IpcMessageKind.Request"/>）。</param>
    /// <param name="callerIsAdministrator">依頼した人が管理者として実行しているかどうか。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>返事（<see cref="IpcMessageKind.Response"/>）。処理に失敗しても例外は投げず、状態で返す。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> が null。</exception>
    /// <exception cref="OperationCanceledException">中止された。</exception>
    public async Task<IpcEnvelope> HandleAsync(IpcEnvelope request, bool callerIsAdministrator, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.IsDefined(request.Operation))
        {
            return Reply(request, IpcResponseStatus.UnknownOperation);
        }

        try
        {
            var payload = request.Operation switch
            {
                SensorOperation.GetCatalog => await GetCatalogAsync(cancellationToken).ConfigureAwait(false),
                SensorOperation.GetLatest => await GetLatestAsync(request, cancellationToken).ConfigureAwait(false),
                SensorOperation.GetRecent => await GetRecentAsync(request, cancellationToken).ConfigureAwait(false),
                SensorOperation.GetHistory => await GetHistoryAsync(request, cancellationToken).ConfigureAwait(false),
                SensorOperation.GetLogPolicy => await GetLogPolicyAsync(request, cancellationToken).ConfigureAwait(false),
                SensorOperation.SetLogPolicy => await SetLogPolicyAsync(request, callerIsAdministrator, cancellationToken).ConfigureAwait(false),
                SensorOperation.GetExtendedSensorState => await GetExtendedStateAsync(cancellationToken).ConfigureAwait(false),
                SensorOperation.GetPawnIoConsent => await GetConsentAsync(cancellationToken).ConfigureAwait(false),
                SensorOperation.SetPawnIoConsent => await SetConsentAsync(request, callerIsAdministrator, cancellationToken).ConfigureAwait(false),
                SensorOperation.GetAppCpuUsage => await GetAppCpuUsageAsync(request, cancellationToken).ConfigureAwait(false),
                SensorOperation.GetAnomalies => await GetAnomaliesAsync(request, cancellationToken).ConfigureAwait(false),
                SensorOperation.SetAnomalyDismissed => await SetAnomalyDismissedAsync(request, cancellationToken).ConfigureAwait(false),
                SensorOperation.MarkAnomalyNotified => await MarkAnomalyNotifiedAsync(request, cancellationToken).ConfigureAwait(false),
                SensorOperation.GetAnomalySettings => await GetAnomalySettingsAsync(cancellationToken).ConfigureAwait(false),
                SensorOperation.SetAnomalySetting => await SetAnomalySettingAsync(request, cancellationToken).ConfigureAwait(false),
                _ => null,
            };

            return payload is null ? Reply(request, IpcResponseStatus.InvalidRequest) : Reply(request, IpcResponseStatus.Ok, payload);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Reply(request, IpcResponseStatus.Failed);
        }
    }

    private static IpcEnvelope Reply(IpcEnvelope request, IpcResponseStatus status, JsonElement? payload = null) =>
        new() { Kind = IpcMessageKind.Response, Id = request.Id, Status = status, Payload = payload };

    private async Task<JsonElement?> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var catalog = await _service.GetCatalogAsync(cancellationToken).ConfigureAwait(false);
        return IpcConnection.ToPayload(catalog.Select(SensorDtoMapper.ToDto).ToArray(), SensorIpcJsonContext.Default.SensorDescriptorDtoArray);
    }

    private async Task<JsonElement?> GetLatestAsync(IpcEnvelope request, CancellationToken cancellationToken)
    {
        var args = IpcConnection.FromPayload(request.Payload, SensorIpcJsonContext.Default.KeysRequest);
        if (args?.Keys is null)
        {
            return null;
        }

        var keys = new List<SensorKey>();
        foreach (var text in args.Keys)
        {
            if (!SensorKey.TryParse(text, out var key))
            {
                return null;
            }

            keys.Add(key);
        }

        var readings = await _service.GetLatestAsync(keys, cancellationToken).ConfigureAwait(false);
        return ToPayload(readings);
    }

    private async Task<JsonElement?> GetRecentAsync(IpcEnvelope request, CancellationToken cancellationToken)
    {
        var args = IpcConnection.FromPayload(request.Payload, SensorIpcJsonContext.Default.RecentRequest);
        if (args is null || !SensorKey.TryParse(args.Key, out var key) || args.WindowMilliseconds <= 0)
        {
            return null;
        }

        var window = TimeSpan.FromMilliseconds(Math.Min(args.WindowMilliseconds, MaxRecentWindow.TotalMilliseconds));
        var readings = await _service.GetRecentAsync(key, window, cancellationToken).ConfigureAwait(false);
        return ToPayload(readings);
    }

    private async Task<JsonElement?> GetHistoryAsync(IpcEnvelope request, CancellationToken cancellationToken)
    {
        var args = IpcConnection.FromPayload(request.Payload, SensorIpcJsonContext.Default.HistoryRequest);
        if (args is null || !SensorKey.TryParse(args.Key, out var key) || args.ToUtc < args.FromUtc || !Enum.IsDefined(args.Resolution))
        {
            return null;
        }

        var points = await _service.GetHistoryAsync(key, args.FromUtc, args.ToUtc, args.Resolution, cancellationToken).ConfigureAwait(false);
        return IpcConnection.ToPayload(points.Select(SensorDtoMapper.ToDto).ToArray(), SensorIpcJsonContext.Default.SensorHistoryPointDtoArray);
    }

    private async Task<JsonElement?> GetLogPolicyAsync(IpcEnvelope request, CancellationToken cancellationToken)
    {
        var args = IpcConnection.FromPayload(request.Payload, SensorIpcJsonContext.Default.KeyRequest);
        if (args is null || !SensorKey.TryParse(args.Key, out var key))
        {
            return null;
        }

        var policy = await _service.GetLogPolicyAsync(key, cancellationToken).ConfigureAwait(false);
        return IpcConnection.ToPayload(
            new LogPolicyResponse(policy is null ? null : SensorDtoMapper.ToDto(policy)),
            SensorIpcJsonContext.Default.LogPolicyResponse);
    }

    private async Task<JsonElement?> SetLogPolicyAsync(IpcEnvelope request, bool callerIsAdministrator, CancellationToken cancellationToken)
    {
        var args = IpcConnection.FromPayload(request.Payload, SensorIpcJsonContext.Default.SetLogPolicyRequest);
        if (args?.Policy is null || !SensorKey.TryParse(args.Key, out var key) || SensorDtoMapper.FromDto(args.Policy) is not { } policy)
        {
            return null;
        }

        var result = callerIsAdministrator
            ? await _service.SetLogPolicyAsync(key, policy, cancellationToken).ConfigureAwait(false)
            : SensorSettingResult.AccessDenied;
        return IpcConnection.ToPayload(new SettingResultResponse(result), SensorIpcJsonContext.Default.SettingResultResponse);
    }

    private async Task<JsonElement?> GetExtendedStateAsync(CancellationToken cancellationToken)
    {
        var state = await _service.GetExtendedSensorStateAsync(cancellationToken).ConfigureAwait(false);
        return IpcConnection.ToPayload(new ExtendedStateMessage(state), SensorIpcJsonContext.Default.ExtendedStateMessage);
    }

    private async Task<JsonElement?> GetConsentAsync(CancellationToken cancellationToken)
    {
        var enabled = await _service.GetPawnIoConsentAsync(cancellationToken).ConfigureAwait(false);
        return IpcConnection.ToPayload(new ConsentMessage(enabled), SensorIpcJsonContext.Default.ConsentMessage);
    }

    private async Task<JsonElement?> SetConsentAsync(IpcEnvelope request, bool callerIsAdministrator, CancellationToken cancellationToken)
    {
        var args = IpcConnection.FromPayload(request.Payload, SensorIpcJsonContext.Default.ConsentMessage);
        if (args is null)
        {
            return null;
        }

        var result = callerIsAdministrator
            ? await _service.SetPawnIoConsentAsync(args.Enabled, cancellationToken).ConfigureAwait(false)
            : SensorSettingResult.AccessDenied;
        return IpcConnection.ToPayload(new SettingResultResponse(result), SensorIpcJsonContext.Default.SettingResultResponse);
    }

    private async Task<JsonElement?> GetAppCpuUsageAsync(IpcEnvelope request, CancellationToken cancellationToken)
    {
        var args = IpcConnection.FromPayload(request.Payload, SensorIpcJsonContext.Default.AppCpuUsageRequest);
        if (args is null
            || args.ToUtc < args.FromUtc
            || args.ToUtc - args.FromUtc > MaxAppUsageRange
            || !double.IsFinite(args.HighLoadThresholdPercent)
            || args.HighLoadThresholdPercent is < 0 or > 100
            || args.Top is <= 0 or > MaxAppUsageTop)
        {
            return null;
        }

        var summary = await _appUsage
            .GetCpuUsageAsync(args.FromUtc, args.ToUtc, args.HighLoadThresholdPercent, args.Top, cancellationToken)
            .ConfigureAwait(false);
        return IpcConnection.ToPayload(AppUsageDtoMapper.ToDto(summary), SensorIpcJsonContext.Default.AppCpuUsageResponse);
    }

    private async Task<JsonElement?> GetAnomaliesAsync(IpcEnvelope request, CancellationToken cancellationToken)
    {
        var args = IpcConnection.FromPayload(request.Payload, SensorIpcJsonContext.Default.AnomaliesRequest);
        if (args is null
            || args.ToUtc < args.FromUtc
            || args.ToUtc - args.FromUtc > MaxAnomalyRange
            || args.Limit is <= 0 or > LocalAnomalyService.MaxLimit)
        {
            return null;
        }

        var anomalies = await _anomalies
            .GetAnomaliesAsync(args.FromUtc, args.ToUtc, args.IncludeDismissed, args.Limit, cancellationToken)
            .ConfigureAwait(false);
        return IpcConnection.ToPayload(anomalies.Select(AnomalyDtoMapper.ToDto).ToArray(), SensorIpcJsonContext.Default.AnomalyDtoArray);
    }

    private async Task<JsonElement?> SetAnomalyDismissedAsync(IpcEnvelope request, CancellationToken cancellationToken)
    {
        var args = IpcConnection.FromPayload(request.Payload, SensorIpcJsonContext.Default.AnomalyDismissRequest);
        if (args is null || args.AnomalyId <= 0)
        {
            return null;
        }

        var result = await _anomalies.SetDismissedAsync(args.AnomalyId, args.Dismissed, cancellationToken).ConfigureAwait(false);
        return IpcConnection.ToPayload(new SettingResultResponse(result), SensorIpcJsonContext.Default.SettingResultResponse);
    }

    private async Task<JsonElement?> MarkAnomalyNotifiedAsync(IpcEnvelope request, CancellationToken cancellationToken)
    {
        var args = IpcConnection.FromPayload(request.Payload, SensorIpcJsonContext.Default.AnomalyIdRequest);
        if (args is null || args.AnomalyId <= 0)
        {
            return null;
        }

        var result = await _anomalies.MarkNotifiedAsync(args.AnomalyId, cancellationToken).ConfigureAwait(false);
        return IpcConnection.ToPayload(new SettingResultResponse(result), SensorIpcJsonContext.Default.SettingResultResponse);
    }

    private async Task<JsonElement?> GetAnomalySettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _anomalies.GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        return IpcConnection.ToPayload(settings.Select(AnomalyDtoMapper.ToDto).ToArray(), SensorIpcJsonContext.Default.AnomalySettingDtoArray);
    }

    private async Task<JsonElement?> SetAnomalySettingAsync(IpcEnvelope request, CancellationToken cancellationToken)
    {
        var args = IpcConnection.FromPayload(request.Payload, SensorIpcJsonContext.Default.AnomalySettingDto);
        if (args is null)
        {
            return null;
        }

        // 知らない種類や範囲の外の値は、変えずに Invalid を返す（画面に理由を出せるように）。
        var result = AnomalyDtoMapper.FromDto(args) is { } setting
            ? await _anomalies.SetSettingAsync(setting, cancellationToken).ConfigureAwait(false)
            : SensorSettingResult.Invalid;
        return IpcConnection.ToPayload(new SettingResultResponse(result), SensorIpcJsonContext.Default.SettingResultResponse);
    }

    private static JsonElement ToPayload(IReadOnlyList<SensorReading> readings) =>
        IpcConnection.ToPayload(readings.Select(SensorDtoMapper.ToDto).ToArray(), SensorIpcJsonContext.Default.SensorReadingDtoArray);
}

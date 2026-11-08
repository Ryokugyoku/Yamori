using Yamori.Core.AppUsage;
using Yamori.Core.Sensors.Ipc.Server;
using Yamori.Core.Sensors.Service;

namespace Yamori.Helper;

/// <summary>
/// Helper の本体。センサーの収集と記録（<see cref="LocalSensorService"/>）、アプリごとの CPU 使用率の記録（<see cref="AppUsageRecording"/>）と、
/// GUI からの接続を受け付けるパイプのサーバーを、止める合図まで動かし続ける。
/// </summary>
public sealed class SensorHostService : BackgroundService
{
    private readonly LocalSensorService _sensors;
    private readonly AppUsageRecording _appUsage;
    private readonly SensorPipeServer _server;
    private readonly ILogger<SensorHostService> _logger;

    /// <summary>
    /// 動かすものを指定して作る。
    /// </summary>
    /// <param name="sensors">収集と記録。</param>
    /// <param name="appUsage">アプリごとの CPU 使用率の記録。</param>
    /// <param name="server">パイプのサーバー。</param>
    /// <param name="logger">ログの出力先。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public SensorHostService(LocalSensorService sensors, AppUsageRecording appUsage, SensorPipeServer server, ILogger<SensorHostService> logger)
    {
        ArgumentNullException.ThrowIfNull(sensors);
        ArgumentNullException.ThrowIfNull(appUsage);
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(logger);
        _sensors = sensors;
        _appUsage = appUsage;
        _server = server;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_sensors.IsPersistenceAvailable)
        {
            _logger.LogWarning("時系列とマスタを開けなかったため、記録せずに動きます。");
        }

        if (!_sensors.IsAnomalyMonitoringAvailable)
        {
            _logger.LogWarning("マスタを開けなかったため、異常の見張りはしません。");
        }

        if (!_appUsage.IsAvailable)
        {
            _logger.LogWarning("アプリごとの使用率の記録を開けなかったため、アプリごとの記録はしません。");
        }

        var collection = _sensors.RunAsync(stoppingToken);
        var appUsage = _appUsage.RunAsync(stoppingToken);
        var server = _server.RunAsync(stoppingToken);
        try
        {
            await Task.WhenAll(collection, appUsage, server).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // パイプを作れない（ほかの Helper が動いているなど）。サービスとしては異常終了させる。
            _logger.LogCritical(ex, "パイプのサーバーを始められませんでした。");
            throw;
        }
    }
}

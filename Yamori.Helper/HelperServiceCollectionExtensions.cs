using Yamori.Core.Anomalies.Detection;
using Yamori.Core.Anomalies.Detection.Throttling;
using Yamori.Core.AppUsage;
using Yamori.Core.Hardware.Wmi;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Extended.Signing;
using Yamori.Core.Sensors.Ipc.Server;
using Yamori.Core.Sensors.Providers.PawnIo;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Storage;

namespace Yamori.Helper;

/// <summary>
/// Helper の部品の組み立て（コンポジションルート）。
/// </summary>
public static class HelperServiceCollectionExtensions
{
    /// <summary>
    /// Helper の部品（データフォルダー、拡張センサーの判断、収集と記録と異常の見張り、アプリごとの使用率の記録、パイプのサーバー、本体）を登録する。
    /// </summary>
    /// <param name="services">登録先。</param>
    /// <returns><paramref name="services"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> が null。</exception>
    public static IServiceCollection AddYamoriHelper(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IExtendedSensorControl>(_ =>
        {
            MachineDataDirectorySecurity.EnsureDirectory(MachineDataPaths.GetSensorDataDirectory());
            return new HelperExtendedSensorControl(
                new RegistryPawnIoInstallationProbe(),
                new PawnIoEditionVerifier(),
                new PawnIoConsentStore(MachineDataPaths.GetPawnIoConsentFilePath()));
        });
        services.AddSingleton(provider => LocalSensorServiceFactory.CreateForHelper(
            new ManagementWmiQueryExecutor(),
            MachineDataPaths.GetSensorPriorityFilePath(),
            MachineDataPaths.GetSensorDataDirectory(),
            provider.GetRequiredService<IExtendedSensorControl>(),
            CreatePawnIoProvider(provider.GetRequiredService<IExtendedSensorControl>(), provider.GetRequiredService<TimeProvider>()),
            provider.GetRequiredService<TimeProvider>(),
            CreateAnomalyRules()));
        services.AddSingleton(provider => AppUsageRecording.CreateForHelper(
            MachineDataPaths.GetSensorDataDirectory(),
            provider.GetRequiredService<TimeProvider>()));
        services.AddSingleton(provider => new SensorPipeServer(
            provider.GetRequiredService<LocalSensorService>(),
            new SensorPipeServerOptions(),
            provider.GetRequiredService<AppUsageRecording>().Service,
            provider.GetRequiredService<LocalSensorService>().Anomalies));
        services.AddHostedService<SensorHostService>();
        return services;
    }

    /// <summary>
    /// 異常の判定の規則を作る。今はサーマルスロットリングだけ（「ふだんより悪い」は段階 5 で足す）。
    /// </summary>
    private static IReadOnlyList<IAnomalyRule> CreateAnomalyRules() => [new ThermalThrottlingRule()];

    /// <summary>
    /// PawnIO の Provider を作る。PawnIO を開くのは、同意があって最初に宣言するときだけ。
    /// </summary>
    private static PawnIoProvider CreatePawnIoProvider(IExtendedSensorControl control, TimeProvider timeProvider) =>
        new(
            control,
            () => PawnIoMsrReader.Open(
                PawnIoInstallLocator.TryFindInstallDirectory() ?? throw new DllNotFoundException("PawnIO を入れたフォルダーが見つかりません。"),
                PawnIoModulePaths.IntelMsr(AppContext.BaseDirectory)),
            Win32CpuCoreLayoutSource.Read,
            isSupportedCpu: null,
            timeProvider);
}

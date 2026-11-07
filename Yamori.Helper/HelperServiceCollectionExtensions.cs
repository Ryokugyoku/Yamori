using Yamori.Core.Hardware.Wmi;
using Yamori.Core.Sensors.Extended;
using Yamori.Core.Sensors.Extended.Signing;
using Yamori.Core.Sensors.Ipc.Server;
using Yamori.Core.Sensors.Service;
using Yamori.Core.Storage;

namespace Yamori.Helper;

/// <summary>
/// Helper の部品の組み立て（コンポジションルート）。
/// </summary>
public static class HelperServiceCollectionExtensions
{
    /// <summary>
    /// Helper の部品（データフォルダー、拡張センサーの判断、収集と記録、パイプのサーバー、本体）を登録する。
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
            extendedProvider: null,
            provider.GetRequiredService<TimeProvider>()));
        services.AddSingleton(provider => new SensorPipeServer(provider.GetRequiredService<LocalSensorService>(), new SensorPipeServerOptions()));
        services.AddHostedService<SensorHostService>();
        return services;
    }
}

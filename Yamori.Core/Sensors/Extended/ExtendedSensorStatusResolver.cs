namespace Yamori.Core.Sensors.Extended;

/// <summary>
/// 拡張センサーの状態を決め、PawnIO の Provider を登録するかどうかを判断する。
/// </summary>
public sealed class ExtendedSensorStatusResolver
{
    private readonly IPawnIoInstallationProbe _probe;

    /// <summary>
    /// 調べ方を指定して作る。
    /// </summary>
    /// <param name="probe">PawnIO が入っているかどうかの調べ方。</param>
    /// <exception cref="ArgumentNullException"><paramref name="probe"/> が null。</exception>
    public ExtendedSensorStatusResolver(IPawnIoInstallationProbe probe)
    {
        ArgumentNullException.ThrowIfNull(probe);
        _probe = probe;
    }

    /// <summary>
    /// 起動時の状態を決める。
    /// </summary>
    /// <param name="canUsePawnIo">PawnIO を使える立場か（Helper のプロセスで、利用に同意済み、公式版を確認済み）。GUI のプロセスでは false。</param>
    /// <returns>状態。<see cref="ExtendedSensorState.Active"/> のときだけ PawnIO の Provider を登録する。</returns>
    public ExtendedSensorState ResolveInitial(bool canUsePawnIo) =>
        !_probe.IsInstalled() ? ExtendedSensorState.NotInstalled
        : canUsePawnIo ? ExtendedSensorState.Active
        : ExtendedSensorState.HelperRequired;

    /// <summary>
    /// 宣言の結果を反映した状態を決める。
    /// </summary>
    /// <param name="initial">起動時の状態。</param>
    /// <param name="discoveryFailed">PawnIO の Provider の宣言が失敗したかどうか。</param>
    /// <returns>宣言に失敗した場合は <see cref="ExtendedSensorState.Unavailable"/>、それ以外は <paramref name="initial"/>。</returns>
    public static ExtendedSensorState AfterDiscovery(ExtendedSensorState initial, bool discoveryFailed) =>
        initial == ExtendedSensorState.Active && discoveryFailed ? ExtendedSensorState.Unavailable : initial;
}

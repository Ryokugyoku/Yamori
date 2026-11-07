using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Sensors.Extended;

/// <summary>
/// GUI のプロセスの中（Helper が動いていない間の代わりの表示）で使う、PawnIO を決して使わない <see cref="IExtendedSensorControl"/>。
/// </summary>
public sealed class OutsideHelperExtendedSensorControl : IExtendedSensorControl
{
    private readonly ExtendedSensorState _state;

    /// <summary>
    /// PawnIO が入っているかどうかの調べ方を指定して作る。調べるのは作ったときの 1 回だけ。
    /// </summary>
    /// <param name="probe">PawnIO が入っているかどうかの調べ方。</param>
    /// <exception cref="ArgumentNullException"><paramref name="probe"/> が null。</exception>
    public OutsideHelperExtendedSensorControl(IPawnIoInstallationProbe probe)
    {
        ArgumentNullException.ThrowIfNull(probe);
        _state = ExtendedSensorStatusResolver.ResolveOutsideHelper(probe.IsInstalled());
    }

    /// <inheritdoc />
    /// <remarks>同意は変わらないため、発生しない。</remarks>
    public event EventHandler? ConsentChanged
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public bool IsConsented => false;

    /// <inheritdoc />
    public bool CanUsePawnIo => false;

    /// <inheritdoc />
    public ExtendedSensorState GetState(bool discoveryFailed) => _state;

    /// <inheritdoc />
    /// <returns>常に <see cref="SensorSettingResult.Unavailable"/>（同意は Helper にだけ保存する）。</returns>
    public SensorSettingResult SetConsent(bool enabled) => SensorSettingResult.Unavailable;
}

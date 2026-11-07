using Yamori.Core.Sensors.Service;

namespace Yamori.Core.Sensors.Extended;

/// <summary>
/// Helper のプロセスの中で使う <see cref="IExtendedSensorControl"/>。PawnIO が入っているか・公式版かを作ったときに調べ、同意はファイルに保存する。
/// </summary>
public sealed class HelperExtendedSensorControl : IExtendedSensorControl
{
    private readonly PawnIoConsentStore _store;
    private readonly bool _installed;
    private readonly bool _official;
    private volatile bool _consented;

    /// <summary>
    /// 調べ方と保存先を指定して作る。
    /// </summary>
    /// <param name="probe">PawnIO が入っているかどうかの調べ方。</param>
    /// <param name="verifier">公式版かどうかの確かめ方。入っている場合だけ使う。</param>
    /// <param name="store">同意の保存先。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public HelperExtendedSensorControl(IPawnIoInstallationProbe probe, IPawnIoEditionVerifier verifier, PawnIoConsentStore store)
    {
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentNullException.ThrowIfNull(verifier);
        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _installed = probe.IsInstalled();
        _official = _installed && verifier.IsOfficial();
        _consented = store.Load();
    }

    /// <inheritdoc />
    public event EventHandler? ConsentChanged;

    /// <inheritdoc />
    public bool IsConsented => _consented;

    /// <inheritdoc />
    public bool CanUsePawnIo => _installed && _official && _consented;

    /// <inheritdoc />
    public ExtendedSensorState GetState(bool discoveryFailed) =>
        ExtendedSensorStatusResolver.ResolveInHelper(_installed, _official, _consented, discoveryFailed);

    /// <inheritdoc />
    public SensorSettingResult SetConsent(bool enabled)
    {
        if (_consented == enabled)
        {
            return SensorSettingResult.Applied;
        }

        try
        {
            _store.Save(enabled);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return SensorSettingResult.Unavailable;
        }

        _consented = enabled;
        ConsentChanged?.Invoke(this, EventArgs.Empty);
        return SensorSettingResult.Applied;
    }
}

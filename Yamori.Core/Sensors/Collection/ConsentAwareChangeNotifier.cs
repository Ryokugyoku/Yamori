using Yamori.Core.Sensors.Extended;

namespace Yamori.Core.Sensors.Collection;

/// <summary>
/// ハードウェアの構成の変化に加え、PawnIO の利用の同意が変わったときにも <see cref="Changed"/> を発生させる。
/// 同意を変えたら、センサーの宣言をやり直して PawnIO のセンサーを出し入れするために使う。
/// </summary>
public sealed class ConsentAwareChangeNotifier : IHardwareChangeNotifier, IDisposable
{
    private readonly IHardwareChangeNotifier _hardware;
    private readonly IExtendedSensorControl _control;

    /// <summary>
    /// 元の通知と、同意の扱いを指定して作る。
    /// </summary>
    /// <param name="hardware">ハードウェアの構成の変化の通知。</param>
    /// <param name="control">拡張センサーの同意の扱い。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public ConsentAwareChangeNotifier(IHardwareChangeNotifier hardware, IExtendedSensorControl control)
    {
        ArgumentNullException.ThrowIfNull(hardware);
        ArgumentNullException.ThrowIfNull(control);
        _hardware = hardware;
        _control = control;
        _hardware.Changed += OnChanged;
        _control.ConsentChanged += OnChanged;
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <summary>
    /// 元の通知の購読を解除する。
    /// </summary>
    public void Dispose()
    {
        _hardware.Changed -= OnChanged;
        _control.ConsentChanged -= OnChanged;
    }

    private void OnChanged(object? sender, EventArgs e) => Changed?.Invoke(this, EventArgs.Empty);
}

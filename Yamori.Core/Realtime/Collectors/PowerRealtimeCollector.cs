using Yamori.Core.Realtime.Sources;

namespace Yamori.Core.Realtime.Collectors;

/// <summary>
/// 電源の状態、バッテリーの充放電、電力量計、GPU の消費電力をまとめて、電源の今の状態を集める。
/// </summary>
public sealed class PowerRealtimeCollector
{
    private readonly IPowerStatusSource _status;
    private readonly IBatteryRateSource _batteries;

    /// <summary>
    /// 電源の状態とバッテリーの値の読み取り元を指定して作る。
    /// </summary>
    /// <param name="status">電源の状態の読み取り元。</param>
    /// <param name="batteries">バッテリーの充放電の読み取り元。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public PowerRealtimeCollector(IPowerStatusSource status, IBatteryRateSource batteries)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(batteries);

        _status = status;
        _batteries = batteries;
    }

    /// <summary>
    /// 電源の今の状態を集める。
    /// </summary>
    /// <param name="meters">電力量計の今の値。</param>
    /// <param name="gpus">GPU ごとの今の状態（消費電力の割合を取り出す）。</param>
    /// <returns>電源の今の状態。</returns>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public PowerRealtime Collect(IReadOnlyList<PowerMeterReading> meters, IReadOnlyList<GpuRealtime> gpus)
    {
        ArgumentNullException.ThrowIfNull(meters);
        ArgumentNullException.ThrowIfNull(gpus);

        var status = _status.Read();

        // バッテリーのない PC では、WMI への問い合わせ自体を省く。
        IReadOnlyList<BatteryRate> batteries = status is { HasBattery: true } ? _batteries.Read() : [];
        var gpuPower = gpus
            .Where(g => g.Telemetry?.PowerPercent is not null)
            .Select(g => new GpuPowerReading(g.Name, g.Telemetry!.PowerPercent!.Value))
            .ToArray();

        return new PowerRealtime(status, batteries, meters, gpuPower);
    }
}

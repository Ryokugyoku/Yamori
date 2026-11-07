using Yamori.Core.Hardware.Wmi;

namespace Yamori.Core.Realtime.Sources;

/// <summary>
/// root\WMI の BatteryStatus クラスから、バッテリーの充放電の速さなどを読み取る。管理者権限は必要ない。
/// </summary>
public sealed class WmiBatteryRateSource : IBatteryRateSource
{
    private const string Query = "SELECT ChargeRate, DischargeRate, Voltage, RemainingCapacity FROM BatteryStatus";

    private readonly IWmiQueryExecutor _wmi;

    /// <summary>
    /// 読み取りに使う WMI 実行器を指定して作る。
    /// </summary>
    /// <param name="wmi">WMI クエリの実行器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="wmi"/> が null。</exception>
    public WmiBatteryRateSource(IWmiQueryExecutor wmi)
    {
        ArgumentNullException.ThrowIfNull(wmi);
        _wmi = wmi;
    }

    /// <inheritdoc />
    public IReadOnlyList<BatteryRate> Read() =>
        _wmi.QueryOrEmpty(WmiNamespaces.Wmi, Query)
            .Select(r => new BatteryRate(
                ChargeRateMilliwatts: r.GetInt32("ChargeRate"),
                DischargeRateMilliwatts: r.GetInt32("DischargeRate"),
                VoltageMillivolts: r.GetInt32("Voltage") is > 0 and var voltage ? voltage : null,
                RemainingCapacityMilliwattHours: r.GetInt32("RemainingCapacity")))
            .ToArray();
}

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// 電源ユニット・バッテリー・今の電源の読み取り結果を、1 つの <see cref="PowerSupplyInfo"/> にまとめる。
/// </summary>
public sealed class PowerSupplyInfoReader : IHardwareReader<PowerSupplyInfo>
{
    private readonly IHardwareReader<IReadOnlyList<PowerSupplyUnitInfo>> _unitReader;
    private readonly IHardwareReader<IReadOnlyList<BatteryInfo>> _batteryReader;
    private readonly IHardwareReader<PowerSource> _sourceReader;

    /// <summary>
    /// それぞれの読み取り処理を指定して作る。
    /// </summary>
    /// <param name="unitReader">電源ユニットの読み取り処理。</param>
    /// <param name="batteryReader">バッテリーの読み取り処理。</param>
    /// <param name="sourceReader">今の電源の読み取り処理。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public PowerSupplyInfoReader(
        IHardwareReader<IReadOnlyList<PowerSupplyUnitInfo>> unitReader,
        IHardwareReader<IReadOnlyList<BatteryInfo>> batteryReader,
        IHardwareReader<PowerSource> sourceReader)
    {
        ArgumentNullException.ThrowIfNull(unitReader);
        ArgumentNullException.ThrowIfNull(batteryReader);
        ArgumentNullException.ThrowIfNull(sourceReader);
        _unitReader = unitReader;
        _batteryReader = batteryReader;
        _sourceReader = sourceReader;
    }

    /// <inheritdoc />
    /// <returns>電源まわりの情報。読めなかった部分は空か <see cref="PowerSource.Unknown"/> になる。</returns>
    public PowerSupplyInfo Read() =>
        new(
            Units: _unitReader.ReadOrDefault([]),
            Batteries: _batteryReader.ReadOrDefault([]),
            Source: _sourceReader.ReadOrDefault(PowerSource.Unknown));
}

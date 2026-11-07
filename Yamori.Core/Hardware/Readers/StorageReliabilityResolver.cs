using Yamori.Core.Hardware.DeviceIo;

namespace Yamori.Core.Hardware.Readers;

/// <summary>
/// WMI（MSFT_StorageReliabilityCounter）の信頼性の情報に、取れなかった温度をディスクへの直接の問い合わせで補う。
/// </summary>
/// <remarks>
/// MSFT_StorageReliabilityCounter は一般ユーザーでは読めないことが多いが、温度だけは IOCTL で読めることが多い。
/// どちらでも取れた場合は WMI の値を使う。
/// </remarks>
public sealed class StorageReliabilityResolver
{
    private readonly Func<IReadOnlyDictionary<int, StorageReliability>> _primary;
    private readonly IStorageTemperatureSource _temperature;

    /// <summary>
    /// 情報源を指定して作る。
    /// </summary>
    /// <param name="primary">ディスクの番号ごとの信頼性の情報を返す処理（WMI）。</param>
    /// <param name="temperature">温度を直接問い合わせる情報源。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public StorageReliabilityResolver(Func<IReadOnlyDictionary<int, StorageReliability>> primary, IStorageTemperatureSource temperature)
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(temperature);
        _primary = primary;
        _temperature = temperature;
    }

    /// <summary>
    /// 指定したディスクの信頼性の情報を読み取る。
    /// </summary>
    /// <param name="diskNumbers">物理ディスクの番号。</param>
    /// <returns>ディスクの番号をキーにした信頼性の情報。何も取れなかったディスクは含まない。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="diskNumbers"/> が null。</exception>
    public IReadOnlyDictionary<int, StorageReliability> Read(IEnumerable<int> diskNumbers)
    {
        ArgumentNullException.ThrowIfNull(diskNumbers);

        var primary = _primary();
        var result = new Dictionary<int, StorageReliability>(primary);
        foreach (var number in diskNumbers.Distinct())
        {
            var known = result.GetValueOrDefault(number);
            if (known?.TemperatureCelsius is not null || _temperature.ReadCelsius(number) is not { } celsius)
            {
                continue;
            }

            result[number] = known is null
                ? new StorageReliability(celsius, null, null)
                : known with { TemperatureCelsius = celsius };
        }

        return result;
    }
}

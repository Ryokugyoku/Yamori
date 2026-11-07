using Yamori.Core.Sensors.Master;
using Yamori.Core.Sensors.TimeSeries;

namespace Yamori.Core.Sensors.Persistence;

/// <summary>
/// カタログ（今見つかっているセンサー）をマスタに反映し、時系列の記録の対象（センサーの番号と記録の設定）を作る。
/// </summary>
public sealed class SensorCatalogSynchronizer
{
    private readonly IHardwareRepository _hardware;
    private readonly ISensorRepository _sensors;
    private readonly ISourceRepository _sources;

    /// <summary>
    /// マスタのリポジトリを指定して作る。
    /// </summary>
    /// <param name="hardware">ハードウェアのリポジトリ。</param>
    /// <param name="sensors">センサーのリポジトリ。</param>
    /// <param name="sources">取得元のリポジトリ。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public SensorCatalogSynchronizer(IHardwareRepository hardware, ISensorRepository sensors, ISourceRepository sources)
    {
        ArgumentNullException.ThrowIfNull(hardware);
        ArgumentNullException.ThrowIfNull(sensors);
        ArgumentNullException.ThrowIfNull(sources);
        _hardware = hardware;
        _sensors = sensors;
        _sources = sources;
    }

    /// <summary>
    /// カタログをマスタに反映する。新しいセンサーには <see cref="DefaultSensorLogPolicies"/> の記録の設定を付ける。
    /// </summary>
    /// <param name="catalog">今のカタログ。</param>
    /// <param name="providerIds">登録しているすべての Provider の ID（フォールバックで値を採用することがあるため、カタログに現れない Provider も含める）。</param>
    /// <param name="nowUtc">今の時刻（ハードウェアを最後に見つけた時刻）。</param>
    /// <returns>キーごとの記録の対象と、Provider の ID ごとの取得元の番号。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="catalog"/> か <paramref name="providerIds"/> が null。</exception>
    public (IReadOnlyDictionary<SensorKey, SensorLogTarget> Targets, IReadOnlyDictionary<string, ushort> SourceIds) Synchronize(
        IReadOnlyList<SensorDescriptor> catalog,
        IEnumerable<string> providerIds,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(providerIds);

        var sourceIds = providerIds
            .Concat(catalog.Select(d => d.ProviderId))
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(id => id, _sources.GetOrCreate, StringComparer.Ordinal);

        var targets = new Dictionary<SensorKey, SensorLogTarget>();
        foreach (var hardware in catalog.GroupBy(d => HardwareRecord.StableKeyOf(d.Key), StringComparer.Ordinal))
        {
            var first = hardware.First();
            var record = _hardware.Upsert(hardware.Key, first.HardwareKind, first.HardwareName, nowUtc);
            foreach (var descriptor in hardware)
            {
                var sensor = _sensors.Ensure(descriptor, record.HardwareId, DefaultSensorLogPolicies.For(descriptor));
                targets[descriptor.Key] = new SensorLogTarget(sensor.SensorId, sensor.LogPolicy);
            }
        }

        return (targets, sourceIds);
    }
}

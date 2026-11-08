using Yamori.Core.Sensors;

namespace Yamori.Core.Anomalies.Detection;

/// <summary>
/// 規則（<see cref="IAnomalyRule"/>）に渡す、1 回分の判定の材料。Helper が毎秒読んだ値を、保存を待たずにメモリ上で渡す。
/// </summary>
public sealed class AnomalyFrame
{
    /// <summary>これより古い値は「今の値」として使わない（読めなくなったセンサーの古い値で判定しないため）。</summary>
    public static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(5);

    private readonly IReadOnlyDictionary<SensorKey, SensorReading> _latest;

    /// <summary>
    /// 材料を指定して作る。
    /// </summary>
    /// <param name="timestampUtc">判定する時刻。</param>
    /// <param name="latest">センサーごとの最新値。</param>
    /// <param name="setting">この規則の異常の種類の設定。</param>
    /// <exception cref="ArgumentNullException"><paramref name="latest"/> か <paramref name="setting"/> が null。</exception>
    public AnomalyFrame(DateTimeOffset timestampUtc, IReadOnlyDictionary<SensorKey, SensorReading> latest, AnomalySetting setting)
    {
        ArgumentNullException.ThrowIfNull(latest);
        ArgumentNullException.ThrowIfNull(setting);
        TimestampUtc = timestampUtc;
        _latest = latest;
        Setting = setting;
    }

    /// <summary>判定する時刻を取得する。</summary>
    public DateTimeOffset TimestampUtc { get; }

    /// <summary>この規則の異常の種類の設定を取得する。</summary>
    public AnomalySetting Setting { get; }

    /// <summary>最新値のあるすべてのセンサーのキーを取得する。</summary>
    public IEnumerable<SensorKey> Keys => _latest.Keys;

    /// <summary>
    /// センサーの今の値を取得する。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <returns>読めていて（<see cref="SensorStatus.Ok"/>）、<see cref="MaxAge"/> より新しい値。なければ null。</returns>
    public double? Value(SensorKey key) =>
        _latest.TryGetValue(key, out var reading)
        && reading.Status == SensorStatus.Ok
        && reading.Value is { } value
        && TimestampUtc - reading.TimestampUtc <= MaxAge
            ? value
            : null;
}

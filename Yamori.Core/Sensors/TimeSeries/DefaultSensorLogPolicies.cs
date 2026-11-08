namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 新しく見つかったセンサーに付ける、記録の初期設定。ユーザーが変えた設定はマスタに残り、こちらは使わない。
/// </summary>
/// <remarks>
/// 細かいセンサー（スレッドごとの値、GPU のエンジンごとの値、PawnIO のコアごとの電圧など）は、記録量を抑えるため初期設定では記録しない。
/// </remarks>
public static class DefaultSensorLogPolicies
{
    /// <summary>既定の記録間隔。</summary>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(10);

    private static readonly HashSet<string> FineGrainedGroups = new(StringComparer.Ordinal) { "threads", "engines", "cores" };

    /// <summary>
    /// センサーの記録の初期設定を決める。
    /// </summary>
    /// <param name="descriptor">センサーの説明。</param>
    /// <returns>初期設定。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="descriptor"/> が null。</exception>
    public static SensorLogPolicy For(SensorDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var enabled = !FineGrainedGroups.Contains(descriptor.Group)
            && descriptor.SensorType is not (SensorType.Count or SensorType.Rate or SensorType.Duration);
        return new SensorLogPolicy(enabled, DefaultInterval, DeadbandFor(descriptor.SensorType));
    }

    /// <summary>
    /// 量の種類ごとの既定のデッドバンド（基準単位）を取得する。
    /// </summary>
    /// <param name="type">量の種類。</param>
    /// <returns>デッドバンド。</returns>
    public static double DeadbandFor(SensorType type) => type switch
    {
        SensorType.Load or SensorType.Level or SensorType.Control => 0.5,  // %
        SensorType.Temperature => 0.5,                                      // ℃
        SensorType.Clock => 10_000_000,                                     // 10 MHz
        SensorType.Power => 0.5,                                            // W
        SensorType.Voltage => 0.005,                                        // 5 mV
        SensorType.Current => 0.05,                                         // A
        SensorType.Fan => 25,                                               // RPM
        SensorType.Data => 1024 * 1024,                                     // 1 MiB
        SensorType.Throughput => 1024,                                      // 1 KiB/s
        SensorType.Energy => 0.1,                                           // Wh
        SensorType.Flag => 0.5,                                             // 0 と 1 が入れ替わったときだけ
        _ => 0,
    };
}

using Yamori.Core.Anomalies.Detection.Usual;
using Yamori.Core.Anomalies.Thresholds;
using Yamori.Core.Sensors;

namespace Yamori.Core.Anomalies;

/// <summary>
/// 異常の設定の初期値と、受け付ける値の範囲。
/// </summary>
public static class AnomalySettingRules
{
    /// <summary>扱うすべての異常の種類を取得する。</summary>
    public static IReadOnlyList<AnomalyKind> Kinds { get; } = Enum.GetValues<AnomalyKind>();

    /// <summary>
    /// 種類の初期の設定（通知はオン、しきい値は自動）を取得する。
    /// </summary>
    /// <param name="kind">異常の種類。</param>
    /// <returns>初期の設定。</returns>
    public static AnomalySetting Default(AnomalyKind kind) => new(kind, NotificationsEnabled: true, ManualThreshold: null);

    /// <summary>
    /// 手動のしきい値として受け付ける範囲を取得する。
    /// </summary>
    /// <param name="kind">異常の種類。</param>
    /// <returns>下限と上限（どちらも含む）。</returns>
    /// <exception cref="ArgumentOutOfRangeException">定義されていない種類。</exception>
    public static (double Min, double Max) ThresholdRange(AnomalyKind kind) => kind switch
    {
        AnomalyKind.ThermalThrottling => (50, 110),   // 注意の温度（℃）
        AnomalyKind.WorseThanUsual => (2, 30),        // 待機中の温度のふだんとの差（℃）
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "定義されていない異常の種類です。"),
    };

    /// <summary>
    /// 自動から手動に切り替えたときに、最初に入れておくしきい値を取得する（自動のときの代表の値）。
    /// </summary>
    /// <param name="kind">異常の種類。</param>
    /// <returns>サーマルスロットリングは CPU の初期の注意の温度（90℃）、ふだんより悪いは自動の温度の差（8℃）。</returns>
    /// <exception cref="ArgumentOutOfRangeException">定義されていない種類。</exception>
    public static double InitialManualThreshold(AnomalyKind kind) => kind switch
    {
        AnomalyKind.ThermalThrottling => TemperatureThresholds.DefaultFor(HardwareKind.Cpu),
        AnomalyKind.WorseThanUsual => UsualComparisonOptions.Default.TemperatureDeltaCelsius,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "定義されていない異常の種類です。"),
    };

    /// <summary>
    /// 手動のしきい値を変えた設定を作る。値は 1℃ 単位に丸め、受け付ける範囲に収める。
    /// </summary>
    /// <param name="setting">今の設定。</param>
    /// <param name="threshold">手動のしきい値。自動に戻すなら null。有限でない値は自動とみなす。</param>
    /// <returns>新しい設定（通知のオン・オフはそのまま）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> が null。</exception>
    /// <exception cref="ArgumentOutOfRangeException">定義されていない種類。</exception>
    public static AnomalySetting WithManualThreshold(AnomalySetting setting, double? threshold)
    {
        ArgumentNullException.ThrowIfNull(setting);
        if (threshold is not { } value || !double.IsFinite(value))
        {
            return setting with { ManualThreshold = null };
        }

        var (min, max) = ThresholdRange(setting.Kind);
        return setting with { ManualThreshold = Math.Clamp(Math.Round(value, MidpointRounding.AwayFromZero), min, max) };
    }

    /// <summary>
    /// 設定を受け付けられるかどうかを調べる。
    /// </summary>
    /// <param name="setting">設定。</param>
    /// <returns>種類が定義済みで、手動のしきい値がないか範囲の中なら true。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> が null。</exception>
    public static bool IsValid(AnomalySetting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);
        if (!Enum.IsDefined(setting.Kind))
        {
            return false;
        }

        if (setting.ManualThreshold is not { } threshold)
        {
            return true;
        }

        var (min, max) = ThresholdRange(setting.Kind);
        return double.IsFinite(threshold) && threshold >= min && threshold <= max;
    }
}

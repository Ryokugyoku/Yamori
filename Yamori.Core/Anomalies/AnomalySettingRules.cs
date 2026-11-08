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
        AnomalyKind.WorseThanUsual => (2, 30),        // ふだんとの差（℃）
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "定義されていない異常の種類です。"),
    };

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

namespace Yamori.Core.Anomalies.Detection.Throttling;

/// <summary>
/// サーマルスロットリングの判定の決まりごと（続いた時間の下限と、重さの境目）。
/// </summary>
/// <remarks>
/// 電力・電流の制限は高負荷のときのふつうの動きでも短く出るため、熱の制限より長く続いたときだけ異常にする。
/// </remarks>
public static class ThrottlingCriteria
{
    /// <summary>熱による制限の印がこれだけ続いたら異常にする。</summary>
    public static readonly TimeSpan ThermalLimitMinimum = TimeSpan.FromSeconds(10);

    /// <summary>注意の温度以上がこれだけ続いたら異常にする。</summary>
    public static readonly TimeSpan AboveThresholdMinimum = TimeSpan.FromSeconds(30);

    /// <summary>電力・電流による制限がこれだけ続いたら異常にする。</summary>
    public static readonly TimeSpan PowerOrCurrentLimitMinimum = TimeSpan.FromSeconds(60);

    /// <summary>異常と判断する前に、材料が途切れても同じ続きとみなす長さ。</summary>
    public static readonly TimeSpan GapTolerance = TimeSpan.FromSeconds(5);

    /// <summary>異常と判断したあと、材料がこれだけ途切れたら終わりにする（終わりかけの出入りで細切れにしないため）。</summary>
    public static readonly TimeSpan ReleaseDelay = TimeSpan.FromSeconds(30);

    /// <summary>1 回の判定で数える時間の上限（判定が遅れたときに、続いた時間を数えすぎないため）。</summary>
    public static readonly TimeSpan MaxTickDuration = TimeSpan.FromSeconds(5);

    /// <summary>警告にする、熱による制限か注意の温度以上が続いた時間。</summary>
    public static readonly TimeSpan WarningThermalDuration = TimeSpan.FromSeconds(60);

    /// <summary>危険にする、熱による制限が続いた時間。</summary>
    public static readonly TimeSpan CriticalThermalDuration = TimeSpan.FromMinutes(5);

    /// <summary>警告にする、クロックの下がり方（%）。</summary>
    public const double WarningClockDropPercent = 25;

    /// <summary>
    /// 電力・電流の制限だけの異常を通知の対象にする、クロックの下がり方（%）。これ未満なら一覧にだけ残す（開発者の判断）。
    /// 熱の制限・注意の温度以上・危険な温度があれば、クロックに関係なく通知の対象にする。
    /// </summary>
    public const double NotifyClockDropPercent = 10;

    /// <summary>Windows の性能の制限（<c>% Performance Limit</c>）がこれ未満なら、電力による制限とみなす（%）。</summary>
    public const double PerformanceLimitPercent = 90;

    /// <summary>クロックの下がり方の基準にする、異常の前の期間。</summary>
    public static readonly TimeSpan BaselineWindow = TimeSpan.FromMinutes(10);
}

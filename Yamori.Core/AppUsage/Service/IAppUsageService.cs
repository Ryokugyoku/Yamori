using Yamori.Core.AppUsage.Analysis;

namespace Yamori.Core.AppUsage.Service;

/// <summary>
/// アプリごとの使用率の記録の窓口。Helper の中では <see cref="LocalAppUsageService"/> が記録を読み、
/// GUI はパイプ（<see cref="Sensors.Ipc.Client.RemoteSensorService"/>）でこれを使う。
/// </summary>
public interface IAppUsageService
{
    /// <summary>
    /// 期間の中で、アプリが CPU をどれだけ使っていたかをまとめて取得する。
    /// </summary>
    /// <param name="fromUtc">期間の始まり（含む）。</param>
    /// <param name="toUtc">期間の終わり（含まない）。</param>
    /// <param name="highLoadThresholdPercent">PC 全体の CPU 使用率がこれを超えていた時間を「高負荷」として別にまとめる（%）。</param>
    /// <param name="top">平均の使用率・高負荷の時間の使用率のそれぞれで返す上位の数。</param>
    /// <param name="cancellationToken">中止の合図。</param>
    /// <returns>まとめ。記録がない、または Helper につながっていない場合は <see cref="AppCpuUsageSummary.Empty"/>。</returns>
    Task<AppCpuUsageSummary> GetCpuUsageAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        double highLoadThresholdPercent,
        int top,
        CancellationToken cancellationToken = default);
}

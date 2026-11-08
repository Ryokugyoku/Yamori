using Yamori.Core.AppUsage.Analysis;
using Yamori.Core.Sensors.Ipc.Messages;

namespace Yamori.Core.Sensors.Ipc;

/// <summary>
/// アプリごとの使用率の記録のまとめと、パイプで送る形（DTO）を相互に変換する。
/// </summary>
public static class AppUsageDtoMapper
{
    /// <summary>
    /// まとめを DTO にする。
    /// </summary>
    /// <param name="summary">まとめ。</param>
    /// <returns>DTO。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="summary"/> が null。</exception>
    public static AppCpuUsageResponse ToDto(AppCpuUsageSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new AppCpuUsageResponse(
            (long)summary.Observed.TotalMilliseconds,
            (long)summary.HighLoad.TotalMilliseconds,
            summary.HighLoadThresholdPercent,
            summary.Apps.Select(a => new AppCpuUsageDto(a.Name, a.AveragePercent, a.HighLoadAveragePercent)).ToArray());
    }

    /// <summary>
    /// DTO をまとめに戻す。名前のないアプリと、数値でない値は捨てる。
    /// </summary>
    /// <param name="dto">DTO。null なら記録なしとする。</param>
    /// <returns>まとめ。</returns>
    public static AppCpuUsageSummary FromDto(AppCpuUsageResponse? dto)
    {
        if (dto is null || dto.ObservedMilliseconds <= 0)
        {
            return AppCpuUsageSummary.Empty;
        }

        var apps = (dto.Apps ?? [])
            .Where(a => a is not null && !string.IsNullOrWhiteSpace(a.Name) && double.IsFinite(a.AveragePercent) && double.IsFinite(a.HighLoadAveragePercent))
            .Select(a => new AppCpuUsage(a.Name, Math.Clamp(a.AveragePercent, 0, 100), Math.Clamp(a.HighLoadAveragePercent, 0, 100)))
            .ToArray();
        return new AppCpuUsageSummary(
            TimeSpan.FromMilliseconds(dto.ObservedMilliseconds),
            TimeSpan.FromMilliseconds(Math.Clamp(dto.HighLoadMilliseconds, 0, dto.ObservedMilliseconds)),
            dto.HighLoadThresholdPercent,
            apps);
    }
}

using Yamori.Core.AppUsage.Analysis;
using Yamori.Core.AppUsage.Storage;

namespace Yamori.Core.AppUsage.Service;

/// <summary>
/// 同じプロセスの中の記録（<see cref="IAppUsageStore"/>）を読む <see cref="IAppUsageService"/>。Helper の中で使う。
/// </summary>
public sealed class LocalAppUsageService : IAppUsageService
{
    private readonly IAppUsageStore? _store;

    /// <summary>
    /// 記録の保存先を指定して作る。
    /// </summary>
    /// <param name="store">記録の保存先。開けなかった場合は null（常に記録なしを返す）。</param>
    public LocalAppUsageService(IAppUsageStore? store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public Task<AppCpuUsageSummary> GetCpuUsageAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        double highLoadThresholdPercent,
        int top,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);
        if (_store is not { } store)
        {
            return Task.FromResult(AppCpuUsageSummary.Empty);
        }

        return Task.Run(
            () => AppCpuUsageSummarizer.Summarize(store.ReadTotals(fromUtc, toUtc, highLoadThresholdPercent), highLoadThresholdPercent, top),
            cancellationToken);
    }
}

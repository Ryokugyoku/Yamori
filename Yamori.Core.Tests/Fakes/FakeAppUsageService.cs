using Yamori.Core.AppUsage.Analysis;
using Yamori.Core.AppUsage.Service;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// 決まったまとめを返し、受けた依頼を覚えておく <see cref="IAppUsageService"/>。
/// </summary>
internal sealed class FakeAppUsageService : IAppUsageService
{
    public AppCpuUsageSummary Summary { get; set; } = AppCpuUsageSummary.Empty;

    public (DateTimeOffset From, DateTimeOffset To, double Threshold, int Top)? LastRequest { get; private set; }

    public Task<AppCpuUsageSummary> GetCpuUsageAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        double highLoadThresholdPercent,
        int top,
        CancellationToken cancellationToken = default)
    {
        LastRequest = (fromUtc, toUtc, highLoadThresholdPercent, top);
        return Task.FromResult(Summary);
    }
}

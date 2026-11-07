using Yamori.Core.Realtime.Sources;

namespace Yamori.Core.Tests.Realtime;

public class PeriodicCacheTests
{
    [Fact]
    public void ReloadsOnlyAfterLifetime()
    {
        var time = new ManualTimeProvider();
        var loads = 0;
        var cache = new PeriodicCache<int>(() => ++loads, TimeSpan.FromSeconds(30), time);

        Assert.Equal(1, cache.Get());
        time.Now += TimeSpan.FromSeconds(29);
        Assert.Equal(1, cache.Get());
        time.Now += TimeSpan.FromSeconds(1);
        Assert.Equal(2, cache.Get());
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 7, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }
}

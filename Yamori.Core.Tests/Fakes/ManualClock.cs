namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// テストから現在時刻を進められる時計。タイマーは実時間のまま。
/// </summary>
internal sealed class ManualClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 7, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;
}

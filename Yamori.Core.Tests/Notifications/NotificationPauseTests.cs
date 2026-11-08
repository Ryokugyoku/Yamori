using Yamori.Core.Notifications;

namespace Yamori.Core.Tests.Notifications;

public sealed class NotificationPauseTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 22, 30, 0, TimeSpan.FromHours(9));

    [Fact]
    public void NotPausedByDefault()
    {
        Assert.False(new NotificationPause().IsPaused(Now));
    }

    [Fact]
    public void PauseForEndsAfterTheDuration()
    {
        var pause = new NotificationPause();
        pause.PauseFor(Now, TimeSpan.FromHours(1));

        Assert.True(pause.IsPaused(Now.AddMinutes(59)));
        Assert.False(pause.IsPaused(Now.AddHours(1)));
        Assert.Null(pause.Until);
    }

    [Fact]
    public void PauseUntilTomorrowEndsAtLocalMidnight()
    {
        var pause = new NotificationPause();
        pause.PauseUntilTomorrow(Now);

        Assert.Equal(new DateTimeOffset(2026, 11, 9, 0, 0, 0, TimeSpan.FromHours(9)), pause.Until);
        Assert.True(pause.IsPaused(Now.AddMinutes(89)));
        Assert.False(pause.IsPaused(Now.AddMinutes(90)));
    }

    [Fact]
    public void ResumeClearsThePause()
    {
        var pause = new NotificationPause();
        pause.PauseFor(Now, TimeSpan.FromHours(1));
        pause.Resume();

        Assert.False(pause.IsPaused(Now));
    }

    [Fact]
    public void NonPositiveDurationIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NotificationPause().PauseFor(Now, TimeSpan.Zero));
    }
}

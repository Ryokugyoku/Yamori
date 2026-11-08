using Yamori.Core.AppUsage;
using Yamori.Core.AppUsage.Recording;
using Yamori.Core.AppUsage.Sampling;
using Yamori.Core.AppUsage.Storage;
using Yamori.Core.Tests.Fakes;

namespace Yamori.Core.Tests.AppUsage;

public class AppCpuRecorderTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);

    private sealed class FixedSampler : IAppCpuSampler
    {
        public double Total { get; set; } = 50;

        public bool Fail { get; set; }

        public AppCpuSample? Sample(DateTimeOffset nowUtc) =>
            Fail ? throw new InvalidOperationException() : new AppCpuSample(nowUtc, Total, [new AppCpuShare("chrome", Total / 2)]);
    }

    private sealed class RecordingStore : IAppUsageStore
    {
        public List<AppCpuMinute> Minutes { get; } = [];

        public List<DateTimeOffset> Cutoffs { get; } = [];

        public bool FailWrites { get; set; }

        public void WriteMinute(AppCpuMinute minute)
        {
            if (FailWrites)
            {
                throw new IOException();
            }

            Minutes.Add(minute);
        }

        public void DeleteBefore(DateTimeOffset cutoffUtc) => Cutoffs.Add(cutoffUtc);

        public AppCpuTotals ReadTotals(DateTimeOffset fromUtc, DateTimeOffset toUtc, double highLoadThresholdPercent) => AppCpuTotals.Empty;
    }

    private static (AppCpuRecorder Recorder, FixedSampler Sampler, RecordingStore Store, ManualClock Clock) Create()
    {
        var sampler = new FixedSampler();
        var store = new RecordingStore();
        var clock = new ManualClock { Now = Start };
        return (new AppCpuRecorder(sampler, store, clock, TimeSpan.FromSeconds(10)), sampler, store, clock);
    }

    [Fact]
    public void WritesEachFinishedMinute()
    {
        var (recorder, sampler, store, clock) = Create();
        for (var i = 0; i < 7; i++)
        {
            sampler.Total = i < 6 ? 60 : 10;
            recorder.Tick();
            clock.Now += TimeSpan.FromSeconds(10);
        }

        var minute = Assert.Single(store.Minutes);
        Assert.Equal(Start, minute.MinuteStartUtc);
        Assert.Equal(6, minute.SampleCount);
        Assert.Equal(60, minute.TotalPercent, 6);
    }

    [Fact]
    public void DropsTheSampleAfterALongGapAndClosesTheMinute()
    {
        var (recorder, sampler, store, clock) = Create();
        recorder.Tick();
        clock.Now += TimeSpan.FromSeconds(10);
        recorder.Tick();

        // スリープから戻った。この回の値は捨てる。
        clock.Now += TimeSpan.FromHours(2);
        sampler.Total = 99;
        recorder.Tick();

        var minute = Assert.Single(store.Minutes);
        Assert.Equal(2, minute.SampleCount);
        Assert.Equal(50, minute.TotalPercent, 6);
    }

    [Fact]
    public void KeepsSamplingWhenWritingOrSamplingFails()
    {
        var (recorder, sampler, store, clock) = Create();
        sampler.Fail = true;
        recorder.Tick();
        sampler.Fail = false;
        store.FailWrites = true;
        for (var i = 0; i < 7; i++)
        {
            clock.Now += TimeSpan.FromSeconds(10);
            recorder.Tick();
        }

        store.FailWrites = false;
        clock.Now += TimeSpan.FromSeconds(60);
        recorder.Tick();
        Assert.Single(store.Minutes);
    }

    [Fact]
    public void DeletesOldMinutesAtMostHourly()
    {
        var (recorder, _, store, clock) = Create();
        recorder.Tick();
        clock.Now += TimeSpan.FromMinutes(30);
        recorder.Tick();
        clock.Now += TimeSpan.FromMinutes(31);
        recorder.Tick();

        Assert.Equal([Start - AppCpuRecorder.DefaultRetention, Start.AddMinutes(61) - AppCpuRecorder.DefaultRetention], store.Cutoffs);
    }

    [Fact]
    public async Task StoppingWritesThePartialMinute()
    {
        var sampler = new FixedSampler();
        var store = new RecordingStore();
        var recorder = new AppCpuRecorder(sampler, store, new ManualClock { Now = Start }, TimeSpan.FromMilliseconds(20));
        using var cancellation = new CancellationTokenSource();

        var run = recorder.RunAsync(cancellation.Token);
        await cancellation.CancelAsync();
        await run.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.NotEmpty(store.Minutes);
    }
}

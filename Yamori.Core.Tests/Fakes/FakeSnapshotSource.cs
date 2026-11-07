using Yamori.Core.Realtime;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// 決めた測定結果を順に返す測定の処理。最後の結果を返し続ける。
/// </summary>
internal sealed class FakeSnapshotSource(params RealtimeSnapshot[] snapshots) : IRealtimeSnapshotSource
{
    private int _next;

    public int TakeCount { get; private set; }

    public bool Disposed { get; private set; }

    public bool ThrowOnTake { get; set; }

    public static RealtimeSnapshot Snapshot(
        CpuRealtime? cpu = null,
        MemoryRealtime? memory = null,
        IReadOnlyList<GpuRealtime>? gpus = null,
        StorageRealtime? storage = null) =>
        new(
            new DateTimeOffset(2026, 11, 7, 0, 0, 0, TimeSpan.Zero),
            cpu ?? new CpuRealtime(),
            memory ?? new MemoryRealtime(),
            gpus ?? [],
            storage ?? new StorageRealtime([], []),
            [],
            new PowerRealtime(null, [], [], []),
            new CoolingRealtime([], []),
            AppsRealtime.Empty);

    public RealtimeSnapshot Take()
    {
        TakeCount++;
        if (ThrowOnTake)
        {
            throw new InvalidOperationException("take failed");
        }

        var snapshot = snapshots[Math.Min(_next, snapshots.Length - 1)];
        _next++;
        return snapshot;
    }

    public void Dispose() => Disposed = true;
}

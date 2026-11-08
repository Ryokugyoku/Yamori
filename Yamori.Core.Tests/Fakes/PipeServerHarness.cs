using Yamori.Core.Sensors.Ipc.Client;
using Yamori.Core.Sensors.Ipc.Server;

namespace Yamori.Core.Tests.Fakes;

/// <summary>
/// テストごとに別の名前のパイプでサーバーを動かし、止めたり動かし直したりできるようにする。
/// </summary>
internal sealed class PipeServerHarness : IAsyncDisposable
{
    private CancellationTokenSource? _cancellation;
    private Task _run = Task.CompletedTask;

    public PipeServerHarness(bool callerIsAdministrator = false)
    {
        Options = new SensorPipeServerOptions
        {
            PipeName = "yamori-test-" + Guid.NewGuid().ToString("N"),
            Security = null,
            IsAdministrator = _ => callerIsAdministrator,
            HandshakeTimeout = TimeSpan.FromSeconds(2),
        };
    }

    public FakeSensorService Service { get; } = new();

    public FakeAppUsageService AppUsage { get; } = new();

    public SensorPipeServerOptions Options { get; }

    public SensorPipeServer? Server { get; private set; }

    public Func<CancellationToken, Task<Stream>> Connector => SensorPipeConnector.Create(Options.PipeName, TimeSpan.FromMilliseconds(200));

    public static RemoteSensorServiceOptions FastRetry { get; } = new()
    {
        RetryDelay = TimeSpan.FromMilliseconds(50),
        IncompatibleRetryDelay = TimeSpan.FromMilliseconds(50),
        HandshakeTimeout = TimeSpan.FromSeconds(2),
        RequestTimeout = TimeSpan.FromSeconds(5),
    };

    public void Start()
    {
        _cancellation = new CancellationTokenSource();
        Server = new SensorPipeServer(Service, Options, AppUsage);
        _run = Server.RunAsync(_cancellation.Token);
    }

    public async Task StopAsync()
    {
        if (_cancellation is null)
        {
            return;
        }

        await _cancellation.CancelAsync();
        await _run.WaitAsync(TimeSpan.FromSeconds(10));
        _cancellation.Dispose();
        _cancellation = null;
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    public static async Task WaitUntilAsync(Func<bool> condition, string because)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException(because);
            }

            await Task.Delay(20);
        }
    }
}

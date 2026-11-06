using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Yamori.Composition;

namespace Yamori;

/// <summary>
/// アプリの起動と終了を管理する。部品の組み立ては <see cref="AppServices"/> に任せる。
/// </summary>
public partial class App : Application
{
    private readonly CancellationTokenSource _monitorCancellation = new();
    private Window? _window;

    /// <summary>
    /// アプリのインスタンスを作る。最初に実行されるユーザーコード。
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// アプリが起動したときに呼ばれる。使用率の記録を始め、メイン画面を表示する。
    /// </summary>
    /// <param name="args">起動要求の詳細。</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var services = AppServices.Create();
        var token = _monitorCancellation.Token;
        _ = Task.Run(() => services.Monitor.RunAsync(token), token);

        var viewModel = services.CreateMainViewModel();
        _window = new MainWindow(viewModel);
        _window.Closed += (_, _) =>
        {
            _monitorCancellation.Cancel();
            viewModel.Dispose();
        };

        _window.Activate();
        _ = viewModel.RefreshCommand.ExecuteAsync(null);
    }
}

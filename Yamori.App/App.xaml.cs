using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Yamori.Composition;
using Yamori.Core.Notifications;
using Yamori.Services;

namespace Yamori;

/// <summary>
/// アプリの起動と終了を管理する。部品の組み立ては <see cref="AppServices"/>、画面の流れは <see cref="AppSession"/> に任せる。
/// </summary>
public partial class App : Application
{
    private readonly NotificationActivationListener _notifications = new();
    private MainWindow? _window;
    private AppSession? _session;

    /// <summary>
    /// アプリのインスタンスを作る。最初に実行されるユーザーコード。
    /// </summary>
    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    /// <summary>
    /// アプリが起動したときに呼ばれる。サインイン画面を表示する。
    /// </summary>
    /// <param name="args">起動要求の詳細。</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // 表示言語を反映してから画面を作るため、部品の組み立てを先に行う。
        var services = AppServices.Create();

        _window = new MainWindow();
        _session = new AppSession(services, _window);
        _window.Closed += (_, _) =>
        {
            _notifications.Activated -= OnNotificationActivated;
            _notifications.Dispose();
            _session.Dispose();
        };

        _session.Start();
        _window.Activate();

        // 通知を押して起動されたときは、サインインのあとでその画面へ移る。
        _notifications.Activated += OnNotificationActivated;
        if (_notifications.Start() is { } request)
        {
            _session.HandleActivation(request);
        }

        // 通知の常駐プログラムを止めていても、アプリを開けば通知が戻るようにする。
        _ = Task.Run(NotifierProcessStarter.EnsureRunning);
    }

    /// <summary>
    /// アプリが開いている間に通知が押されたとき、ウィンドウを前に出して、その画面へ移る。
    /// </summary>
    private void OnNotificationActivated(object? sender, NotificationActivationRequest request)
    {
        _window?.DispatcherQueue.TryEnqueue(() =>
        {
            _session?.HandleActivation(request);
            _window.Activate();
        });
    }

    /// <summary>
    /// UI スレッドで処理されなかった例外を書き残し、アプリを落とさずに続ける。
    /// 見守りのアプリが一時的な読み取りの失敗で消えてしまうより、続けて次の更新で回復するほうがよいため。
    /// </summary>
    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        ErrorLog.Write("UI", e.Exception);
        e.Handled = true;
    }

    /// <summary>
    /// 待たれずに失敗したタスクの例外を書き残す。
    /// </summary>
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ErrorLog.Write("Task", e.Exception);
        e.SetObserved();
    }
}

using System.Globalization;
using Yamori.Core.Notifications;
using Yamori.Core.Sensors.Ipc.Client;
using Yamori.Notifier.Helper;
using Yamori.Notifier.Interop;
using Yamori.Notifier.Localization;
using Yamori.Notifier.Toasts;
using Yamori.Notifier.Tray;
using Yamori.Notifier.Windowing;

namespace Yamori.Notifier;

/// <summary>
/// 通知専用の常駐プログラムの本体。トレイのアイコンとメニュー、通知の一時停止をまとめ、メッセージ ループを回す。
/// </summary>
/// <remarks>
/// Helper のパイプ（異常の知らせだけ）は別のスレッドで動かし、通知とトレイの表示の更新は <see cref="MessageWindow.Invoke"/> で
/// UI スレッドに渡す。
/// </remarks>
internal sealed class NotifierApplication : IDisposable
{
    /// <summary>トレイのアイコンのファイル（パッケージのフォルダーからの相対パス）。アプリ本体のウィンドウのアイコンと同じもの。</summary>
    private const string IconRelativePath = @"Assets\Logo\Yamori.ico";

    private readonly MessageWindow _window;
    private readonly TrayIcon _tray;
    private readonly NotificationPause _pause = new();
    private readonly NotificationGate _gate;
    private readonly HelperAnomalyListener _listener;
    private HelperConnectionState _helperState = HelperConnectionState.Connecting;

    /// <summary>
    /// 隠しウィンドウとトレイのアイコンを作り、Helper のパイプにつなぎ始める。UI スレッドから呼ぶこと。
    /// </summary>
    public NotifierApplication()
    {
        _window = new MessageWindow();
        _tray = new TrayIcon(_window, Path.Combine(AppContext.BaseDirectory, IconRelativePath), NotifierText.Get("Tray_Tooltip"));
        _tray.Invoked += OnTrayInvoked;
        _tray.MenuRequested += OnTrayMenuRequested;
        _gate = new NotificationGate(_pause, UserPresence.IsBusy, () => DateTimeOffset.Now);

        var sink = new WindowToastSink(
            _window,
            _gate,
            new AnomalyToastComposer(NotifierToastTemplates.Create(), CultureInfo.CurrentCulture),
            new ToastPresenter());
        _listener = new HelperAnomalyListener(remote => new AnomalyNotificationDispatcher(remote, new AnomalyToastFilter(), sink, TimeProvider.System));
        _listener.ConnectionStateChanged += OnHelperConnectionStateChanged;
        _listener.Start();
    }

    /// <summary>
    /// メッセージ ループを回す。「終了」を選ぶまで戻らない。
    /// </summary>
    public void Run() => _window.Run();

    /// <summary>
    /// Helper のパイプを閉じ、トレイのアイコンを消し、隠しウィンドウを壊す。
    /// </summary>
    public void Dispose()
    {
        _listener.ConnectionStateChanged -= OnHelperConnectionStateChanged;
        _listener.Dispose();
        _tray.Invoked -= OnTrayInvoked;
        _tray.MenuRequested -= OnTrayMenuRequested;
        _tray.Dispose();
        _window.Dispose();
    }

    private void OnTrayInvoked(object? sender, EventArgs e) => AppLauncher.OpenApp();

    private void OnTrayMenuRequested(object? sender, NativeMethods.Point position)
    {
        var paused = _pause.IsPaused(DateTimeOffset.Now);
        TrayMenuItem[] items =
        [
            new(TrayCommand.OpenApp, NotifierText.Get("Tray_Open")),
            TrayMenuItem.Separator,
            new(TrayCommand.PauseOneHour, NotifierText.Get("Tray_PauseOneHour")),
            new(TrayCommand.PauseUntilTomorrow, NotifierText.Get("Tray_PauseUntilTomorrow")),
            new(TrayCommand.Resume, NotifierText.Get("Tray_Resume"), IsEnabled: paused),
            TrayMenuItem.Separator,
            new(TrayCommand.Exit, NotifierText.Get("Tray_Exit")),
        ];

        switch (TrayMenu.Show(_window, position, items))
        {
            case TrayCommand.OpenApp:
                AppLauncher.OpenApp();
                break;
            case TrayCommand.PauseOneHour:
                _pause.PauseFor(DateTimeOffset.Now, TimeSpan.FromHours(1));
                break;
            case TrayCommand.PauseUntilTomorrow:
                _pause.PauseUntilTomorrow(DateTimeOffset.Now);
                break;
            case TrayCommand.Resume:
                _pause.Resume();
                break;
            case TrayCommand.Exit:
                _window.Quit();
                return;
        }

        UpdateTooltip();
    }

    /// <summary>パイプのスレッドから呼ばれる。トレイの表示は UI スレッドで変える。</summary>
    private void OnHelperConnectionStateChanged(object? sender, HelperConnectionState state) =>
        _window.Invoke(() =>
        {
            _helperState = state;
            UpdateTooltip();
        });

    private void UpdateTooltip()
    {
        string tooltip;
        if (_pause.IsPaused(DateTimeOffset.Now) && _pause.Until is { } until)
        {
            tooltip = NotifierText.Format("Tray_TooltipPaused", until.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
        }
        else if (_helperState is HelperConnectionState.NotRunning or HelperConnectionState.Incompatible)
        {
            tooltip = NotifierText.Get("Tray_TooltipDisconnected");
        }
        else
        {
            tooltip = NotifierText.Get("Tray_Tooltip");
        }

        _tray.SetTooltip(tooltip);
    }
}

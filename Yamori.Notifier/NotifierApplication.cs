using System.Globalization;
using Yamori.Core.Notifications;
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
/// Helper から異常の知らせを受け取って通知を出す部分は、段階 3（通知専用の常駐プログラム）で足す。
/// 今は、トレイと通知の出し方・押したときの動きを確かめるための土台。
/// </remarks>
internal sealed class NotifierApplication : IDisposable
{
    /// <summary>トレイのアイコンのファイル（パッケージのフォルダーからの相対パス）。アプリ本体のウィンドウのアイコンと同じもの。</summary>
    private const string IconRelativePath = @"Assets\Logo\Yamori.ico";

    private readonly MessageWindow _window;
    private readonly TrayIcon _tray;
    private readonly NotificationPause _pause = new();

    /// <summary>
    /// 隠しウィンドウとトレイのアイコンを作る。UI スレッドから呼ぶこと。
    /// </summary>
    public NotifierApplication()
    {
        _window = new MessageWindow();
        _tray = new TrayIcon(_window, Path.Combine(AppContext.BaseDirectory, IconRelativePath), NotifierText.Get("Tray_Tooltip"));
        _tray.Invoked += OnTrayInvoked;
        _tray.MenuRequested += OnTrayMenuRequested;
        Gate = new NotificationGate(_pause, UserPresence.IsBusy, () => DateTimeOffset.Now);
    }

    /// <summary>今、通知を出してよいかを決める部品を取得する。</summary>
    public NotificationGate Gate { get; }

    /// <summary>
    /// メッセージ ループを回す。「終了」を選ぶまで戻らない。
    /// </summary>
    public void Run() => _window.Run();

    /// <summary>
    /// トレイのアイコンを消し、隠しウィンドウを壊す。
    /// </summary>
    public void Dispose()
    {
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

    private void UpdateTooltip()
    {
        var tooltip = _pause.IsPaused(DateTimeOffset.Now) && _pause.Until is { } until
            ? NotifierText.Format("Tray_TooltipPaused", until.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
            : NotifierText.Get("Tray_Tooltip");
        _tray.SetTooltip(tooltip);
    }
}

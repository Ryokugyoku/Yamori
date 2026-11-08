using Yamori.Notifier.Interop;
using Yamori.Notifier.Windowing;

namespace Yamori.Notifier.Tray;

/// <summary>
/// タスクバーの通知領域（トレイ）のアイコン。左クリックと右クリック（メニューの要求）を知らせる。
/// エクスプローラーが再起動したときは、アイコンを自動で出し直す。
/// </summary>
internal sealed unsafe class TrayIcon : IDisposable
{
    private const uint IconId = 1;

    /// <summary>アイコンのクリックなどを知らせるメッセージ。</summary>
    private const uint WmTrayCallback = NativeMethods.WmApp + 1;

    private readonly MessageWindow _window;
    private readonly uint _taskbarCreatedMessage;
    private readonly nint _icon;
    private string _tooltip;
    private bool _added;

    /// <summary>
    /// アイコンを作ってトレイに出す。UI スレッドから呼ぶこと。
    /// </summary>
    /// <param name="window">クリックなどを受け取る隠しウィンドウ。</param>
    /// <param name="iconPath">アイコンのファイル（.ico）のパス。読めなければ Windows の既定のアイコンになる。</param>
    /// <param name="tooltip">マウスを重ねたときに出す文字。</param>
    /// <exception cref="ArgumentNullException"><paramref name="window"/> が null。</exception>
    /// <exception cref="ArgumentException"><paramref name="iconPath"/> か <paramref name="tooltip"/> が空。</exception>
    public TrayIcon(MessageWindow window, string iconPath, string tooltip)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(tooltip);

        _window = window;
        _tooltip = tooltip;
        _icon = NativeMethods.LoadImage(
            0,
            iconPath,
            NativeMethods.ImageIcon,
            NativeMethods.GetSystemMetrics(NativeMethods.SmCxSmIcon),
            NativeMethods.GetSystemMetrics(NativeMethods.SmCySmIcon),
            NativeMethods.LrLoadFromFile);

        // エクスプローラーが再起動するとトレイが作り直され、アイコンが消える。この知らせで出し直す。
        _taskbarCreatedMessage = NativeMethods.RegisterWindowMessage("TaskbarCreated");
        _window.MessageReceived += OnMessageReceived;
        Add();
    }

    /// <summary>アイコンを左クリック（またはキーボードで選択）したときに起きる。</summary>
    public event EventHandler? Invoked;

    /// <summary>アイコンを右クリックしてメニューを求めたときに起きる。引数はメニューを出す画面上の位置。</summary>
    public event EventHandler<NativeMethods.Point>? MenuRequested;

    /// <summary>
    /// マウスを重ねたときに出す文字を変える。
    /// </summary>
    /// <param name="tooltip">新しい文字。</param>
    /// <exception cref="ArgumentException"><paramref name="tooltip"/> が空。</exception>
    public void SetTooltip(string tooltip)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tooltip);
        _tooltip = tooltip;

        var data = CreateData();
        NativeMethods.ShellNotifyIcon(NativeMethods.NimModify, &data);
    }

    /// <summary>
    /// アイコンをトレイから消し、読み込んだ画像を捨てる。
    /// </summary>
    public void Dispose()
    {
        _window.MessageReceived -= OnMessageReceived;
        if (_added)
        {
            var data = CreateData();
            NativeMethods.ShellNotifyIcon(NativeMethods.NimDelete, &data);
            _added = false;
        }

        if (_icon != 0)
        {
            NativeMethods.DestroyIcon(_icon);
        }
    }

    private void Add()
    {
        var data = CreateData();
        _added = NativeMethods.ShellNotifyIcon(NativeMethods.NimAdd, &data);
        if (_added)
        {
            // 版 4 にすると、右クリックが WM_CONTEXTMENU で届き、位置も一緒に届く。
            data.TimeoutOrVersion = NativeMethods.NotifyIconVersion4;
            NativeMethods.ShellNotifyIcon(NativeMethods.NimSetVersion, &data);
        }
    }

    private NativeMethods.NotifyIconData CreateData()
    {
        var data = new NativeMethods.NotifyIconData
        {
            Size = (uint)sizeof(NativeMethods.NotifyIconData),
            Window = _window.Handle,
            Id = IconId,
            Flags = NativeMethods.NifMessage | NativeMethods.NifTip | NativeMethods.NifShowTip
                | (_icon != 0 ? NativeMethods.NifIcon : 0),
            CallbackMessage = WmTrayCallback,
            Icon = _icon,
        };
        NativeMethods.CopyToBuffer(_tooltip, data.Tip, 128);
        return data;
    }

    private void OnMessageReceived(object? sender, WindowMessageEventArgs e)
    {
        if (e.Message == _taskbarCreatedMessage && _taskbarCreatedMessage != 0)
        {
            Add();
            e.Handled = true;
            return;
        }

        if (e.Message != WmTrayCallback)
        {
            return;
        }

        // 版 4 では、lParam の下位 16 ビットが出来事、wParam が画面上の位置（横・縦）。
        var notification = (uint)(e.LParam & 0xFFFF);
        switch (notification)
        {
            case NativeMethods.NinSelect:
            case NativeMethods.NinKeySelect:
                Invoked?.Invoke(this, EventArgs.Empty);
                break;
            case NativeMethods.WmContextMenu:
                var point = new NativeMethods.Point
                {
                    X = (short)(e.WParam & 0xFFFF),
                    Y = (short)((e.WParam >> 16) & 0xFFFF),
                };
                MenuRequested?.Invoke(this, point);
                break;
        }

        e.Handled = true;
    }
}

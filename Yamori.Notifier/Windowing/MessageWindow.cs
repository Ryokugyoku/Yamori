using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Yamori.Notifier.Interop;

namespace Yamori.Notifier.Windowing;

/// <summary>
/// 画面に出さない隠しウィンドウと、そのメッセージの繰り返し（メッセージ ループ）。
/// トレイのアイコンのクリックを受け取り、別のスレッドからの処理を UI スレッドで動かす。1 つのプロセスに 1 つだけ作る。
/// </summary>
internal sealed unsafe class MessageWindow : IDisposable
{
    private const string ClassName = "Yamori.Notifier.MessageWindow";

    /// <summary>別のスレッドから頼まれた処理があることを知らせるメッセージ。</summary>
    private const uint WmInvoke = NativeMethods.WmApp + 2;

    private static MessageWindow? s_current;

    private readonly ConcurrentQueue<Action> _pending = new();
    private readonly nint _instance;
    private readonly nint _classNameBuffer;
    private nint _handle;

    /// <summary>
    /// 隠しウィンドウを作る。メッセージ ループを回すスレッド（UI スレッド）から呼ぶこと。
    /// </summary>
    /// <exception cref="InvalidOperationException">すでに作ってある、またはウィンドウを作れなかった。</exception>
    public MessageWindow()
    {
        if (s_current is not null)
        {
            throw new InvalidOperationException("隠しウィンドウは 1 つしか作れません。");
        }

        _instance = NativeMethods.GetModuleHandle(null);
        _classNameBuffer = Marshal.StringToHGlobalUni(ClassName);

        var windowClass = new NativeMethods.WindowClass
        {
            Size = (uint)sizeof(NativeMethods.WindowClass),
            WindowProcedure = &WindowProcedure,
            Instance = _instance,
            ClassName = (char*)_classNameBuffer,
        };

        if (NativeMethods.RegisterClassEx(&windowClass) == 0)
        {
            Marshal.FreeHGlobal(_classNameBuffer);
            throw new InvalidOperationException($"ウィンドウ クラスを登録できませんでした（{Marshal.GetLastPInvokeError()}）。");
        }

        // 作る途中で届くメッセージも受け取れるよう、作る前に登録する。
        s_current = this;
        _handle = NativeMethods.CreateWindowEx(0, ClassName, "Yamori Notifier", 0, 0, 0, 0, 0, 0, 0, _instance, 0);
        if (_handle == 0)
        {
            var error = Marshal.GetLastPInvokeError();
            s_current = null;
            NativeMethods.UnregisterClass(ClassName, _instance);
            Marshal.FreeHGlobal(_classNameBuffer);
            throw new InvalidOperationException($"隠しウィンドウを作れませんでした（{error}）。");
        }
    }

    /// <summary>
    /// ウィンドウにメッセージが届いたときに起きる。処理したら <see cref="WindowMessageEventArgs.Handled"/> を true にする。
    /// </summary>
    public event EventHandler<WindowMessageEventArgs>? MessageReceived;

    /// <summary>隠しウィンドウのハンドルを取得する。</summary>
    public nint Handle => _handle;

    /// <summary>
    /// メッセージ ループを回す。<see cref="Quit"/> が呼ばれるまで戻らない。
    /// </summary>
    public void Run()
    {
        NativeMethods.Msg message;
        while (NativeMethods.GetMessage(&message, 0, 0, 0) > 0)
        {
            NativeMethods.TranslateMessage(&message);
            NativeMethods.DispatchMessage(&message);
        }
    }

    /// <summary>
    /// メッセージ ループを終わらせる。どのスレッドから呼んでもよい。
    /// </summary>
    public void Quit() => Invoke(() => NativeMethods.PostQuitMessage(0));

    /// <summary>
    /// 処理を UI スレッドで動かすよう頼む。どのスレッドから呼んでもよく、待たずに戻る。
    /// </summary>
    /// <param name="action">動かす処理。</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> が null。</exception>
    public void Invoke(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _pending.Enqueue(action);
        NativeMethods.PostMessage(_handle, WmInvoke, 0, 0);
    }

    /// <summary>
    /// 隠しウィンドウを壊し、ウィンドウ クラスの登録を解く。
    /// </summary>
    public void Dispose()
    {
        if (_handle == 0)
        {
            return;
        }

        NativeMethods.DestroyWindow(_handle);
        _handle = 0;
        s_current = null;
        NativeMethods.UnregisterClass(ClassName, _instance);
        Marshal.FreeHGlobal(_classNameBuffer);
    }

    [UnmanagedCallersOnly]
    private static nint WindowProcedure(nint window, uint message, nint wParam, nint lParam)
    {
        try
        {
            if (s_current is { } current && current.ProcessMessage(message, wParam, lParam))
            {
                return 0;
            }
        }
        catch (Exception ex)
        {
            // Win32 の呼び出し元へ例外を戻すとプロセスが落ちるため、ここで止める。
            NotifierLog.Write("WindowProcedure", ex);
        }

        return NativeMethods.DefWindowProc(window, message, wParam, lParam);
    }

    private bool ProcessMessage(uint message, nint wParam, nint lParam)
    {
        if (message == WmInvoke)
        {
            while (_pending.TryDequeue(out var action))
            {
                action();
            }

            return true;
        }

        var args = new WindowMessageEventArgs(message, wParam, lParam);
        MessageReceived?.Invoke(this, args);
        return args.Handled;
    }
}

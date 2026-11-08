using System.Runtime.InteropServices;

namespace Yamori.Notifier.Interop;

/// <summary>
/// 通知専用の常駐プログラムが使う Win32 の関数・定数・構造体（隠しウィンドウ、メッセージの処理、トレイのアイコン、メニュー、利用者の状態）。
/// 構造体はすべてポインターで渡し、実行時のマーシャリングを使わない。
/// </summary>
internal static unsafe partial class NativeMethods
{
    /// <summary>何もしないメッセージ（WM_NULL）。メニューを閉じたあとの決まりごとに使う。</summary>
    public const uint WmNull = 0x0000;

    /// <summary>ウィンドウが壊される（WM_DESTROY）。</summary>
    public const uint WmDestroy = 0x0002;

    /// <summary>右クリックやキーボードでの、コンテキスト メニューの要求（WM_CONTEXTMENU）。</summary>
    public const uint WmContextMenu = 0x007B;

    /// <summary>アプリが自由に使えるメッセージの先頭（WM_APP）。</summary>
    public const uint WmApp = 0x8000;

    /// <summary>トレイのアイコンを左クリックで選んだ（NIN_SELECT）。</summary>
    public const uint NinSelect = 0x0400;

    /// <summary>トレイのアイコンをキーボードで選んだ（NIN_KEYSELECT）。</summary>
    public const uint NinKeySelect = 0x0401;

    /// <summary>トレイにアイコンを足す（NIM_ADD）。</summary>
    public const uint NimAdd = 0x0000;

    /// <summary>トレイのアイコンを変える（NIM_MODIFY）。</summary>
    public const uint NimModify = 0x0001;

    /// <summary>トレイのアイコンを消す（NIM_DELETE）。</summary>
    public const uint NimDelete = 0x0002;

    /// <summary>トレイのアイコンの動き方の版を決める（NIM_SETVERSION）。</summary>
    public const uint NimSetVersion = 0x0004;

    /// <summary>新しい動き方（NOTIFYICON_VERSION_4）。クリックの位置が wParam で届く。</summary>
    public const uint NotifyIconVersion4 = 4;

    /// <summary>uCallbackMessage が有効（NIF_MESSAGE）。</summary>
    public const uint NifMessage = 0x0001;

    /// <summary>hIcon が有効（NIF_ICON）。</summary>
    public const uint NifIcon = 0x0002;

    /// <summary>szTip が有効（NIF_TIP）。</summary>
    public const uint NifTip = 0x0004;

    /// <summary>版 4 でも標準のツールチップを出す（NIF_SHOWTIP）。</summary>
    public const uint NifShowTip = 0x0080;

    /// <summary>文字のメニュー項目（MF_STRING）。</summary>
    public const uint MfString = 0x0000;

    /// <summary>押せないメニュー項目（MF_GRAYED）。</summary>
    public const uint MfGrayed = 0x0001;

    /// <summary>メニューの区切り線（MF_SEPARATOR）。</summary>
    public const uint MfSeparator = 0x0800;

    /// <summary>選んだ項目の番号を戻り値で返す（TPM_RETURNCMD）。</summary>
    public const uint TpmReturnCmd = 0x0100;

    /// <summary>右クリックでも選べる（TPM_RIGHTBUTTON）。</summary>
    public const uint TpmRightButton = 0x0002;

    /// <summary>アイコンの画像（IMAGE_ICON）。</summary>
    public const uint ImageIcon = 1;

    /// <summary>ファイルから読み込む（LR_LOADFROMFILE）。</summary>
    public const uint LrLoadFromFile = 0x0010;

    /// <summary>小さいアイコンの幅（SM_CXSMICON）。</summary>
    public const int SmCxSmIcon = 49;

    /// <summary>小さいアイコンの高さ（SM_CYSMICON）。</summary>
    public const int SmCySmIcon = 50;

    /// <summary>利用者がいない（QUNS_NOT_PRESENT）。画面のロック中など。</summary>
    public const int QunsNotPresent = 1;

    /// <summary>全画面のアプリを使っている（QUNS_BUSY）。</summary>
    public const int QunsBusy = 2;

    /// <summary>Direct3D の全画面（ゲームなど）を使っている（QUNS_RUNNING_D3D_FULL_SCREEN）。</summary>
    public const int QunsRunningD3dFullScreen = 3;

    /// <summary>プレゼンテーションの設定中（QUNS_PRESENTATION_MODE）。</summary>
    public const int QunsPresentationMode = 4;

    /// <summary>
    /// 画面上の点（POINT）。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        /// <summary>横の位置。</summary>
        public int X;

        /// <summary>縦の位置。</summary>
        public int Y;
    }

    /// <summary>
    /// ウィンドウのメッセージ（MSG）。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Msg
    {
        /// <summary>宛先のウィンドウ。</summary>
        public nint Window;

        /// <summary>メッセージの番号。</summary>
        public uint Message;

        /// <summary>1 つ目の値。</summary>
        public nint WParam;

        /// <summary>2 つ目の値。</summary>
        public nint LParam;

        /// <summary>送られた時刻。</summary>
        public uint Time;

        /// <summary>送られたときのマウスの位置。</summary>
        public Point Position;

        /// <summary>予約。</summary>
        public uint Private;
    }

    /// <summary>
    /// ウィンドウ クラスの登録内容（WNDCLASSEXW）。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WindowClass
    {
        /// <summary>この構造体の大きさ。</summary>
        public uint Size;

        /// <summary>クラスの形式。</summary>
        public uint Style;

        /// <summary>メッセージを受け取る関数。</summary>
        public delegate* unmanaged<nint, uint, nint, nint, nint> WindowProcedure;

        /// <summary>クラスの追加の領域。</summary>
        public int ClassExtra;

        /// <summary>ウィンドウの追加の領域。</summary>
        public int WindowExtra;

        /// <summary>モジュールのハンドル。</summary>
        public nint Instance;

        /// <summary>アイコン。</summary>
        public nint Icon;

        /// <summary>カーソル。</summary>
        public nint Cursor;

        /// <summary>背景のブラシ。</summary>
        public nint Background;

        /// <summary>メニューの名前。</summary>
        public char* MenuName;

        /// <summary>クラスの名前。</summary>
        public char* ClassName;

        /// <summary>小さいアイコン。</summary>
        public nint SmallIcon;
    }

    /// <summary>
    /// トレイのアイコンの内容（NOTIFYICONDATAW）。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NotifyIconData
    {
        /// <summary>この構造体の大きさ。</summary>
        public uint Size;

        /// <summary>クリックなどを受け取るウィンドウ。</summary>
        public nint Window;

        /// <summary>アイコンの番号。</summary>
        public uint Id;

        /// <summary>どの項目が有効か（NIF_*）。</summary>
        public uint Flags;

        /// <summary>クリックなどを知らせるメッセージの番号。</summary>
        public uint CallbackMessage;

        /// <summary>アイコン。</summary>
        public nint Icon;

        /// <summary>ツールチップの文字（終端を含め 128 文字まで）。</summary>
        public fixed char Tip[128];

        /// <summary>状態。</summary>
        public uint State;

        /// <summary>変える状態。</summary>
        public uint StateMask;

        /// <summary>吹き出しの本文（使わない）。</summary>
        public fixed char Info[256];

        /// <summary>動き方の版（NIM_SETVERSION のとき）。</summary>
        public uint TimeoutOrVersion;

        /// <summary>吹き出しの見出し（使わない）。</summary>
        public fixed char InfoTitle[64];

        /// <summary>吹き出しの形式（使わない）。</summary>
        public uint InfoFlags;

        /// <summary>アイコンの GUID（使わない）。</summary>
        public Guid GuidItem;

        /// <summary>吹き出しのアイコン（使わない）。</summary>
        public nint BalloonIcon;
    }

    /// <summary>モジュールのハンドルを取得する（GetModuleHandleW）。</summary>
    /// <param name="moduleName">モジュールの名前。null なら今の実行ファイル。</param>
    /// <returns>モジュールのハンドル。</returns>
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint GetModuleHandle(string? moduleName);

    /// <summary>ウィンドウ クラスを登録する（RegisterClassExW）。</summary>
    /// <param name="windowClass">登録内容。</param>
    /// <returns>クラスの番号。失敗したら 0。</returns>
    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW", SetLastError = true)]
    public static partial ushort RegisterClassEx(WindowClass* windowClass);

    /// <summary>ウィンドウ クラスの登録を解く（UnregisterClassW）。</summary>
    /// <param name="className">クラスの名前。</param>
    /// <param name="instance">モジュールのハンドル。</param>
    /// <returns>成功したら true。</returns>
    [LibraryImport("user32.dll", EntryPoint = "UnregisterClassW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterClass(string className, nint instance);

    /// <summary>ウィンドウを作る（CreateWindowExW）。</summary>
    /// <param name="exStyle">拡張の形式。</param>
    /// <param name="className">クラスの名前。</param>
    /// <param name="windowName">ウィンドウの名前。</param>
    /// <param name="style">形式。</param>
    /// <param name="x">横の位置。</param>
    /// <param name="y">縦の位置。</param>
    /// <param name="width">幅。</param>
    /// <param name="height">高さ。</param>
    /// <param name="parent">親のウィンドウ。</param>
    /// <param name="menu">メニュー。</param>
    /// <param name="instance">モジュールのハンドル。</param>
    /// <param name="parameter">作るときに渡す値。</param>
    /// <returns>ウィンドウのハンドル。失敗したら 0。</returns>
    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateWindowEx(
        uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height,
        nint parent, nint menu, nint instance, nint parameter);

    /// <summary>ウィンドウを壊す（DestroyWindow）。</summary>
    /// <param name="window">ウィンドウ。</param>
    /// <returns>成功したら true。</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyWindow(nint window);

    /// <summary>既定のメッセージの処理（DefWindowProcW）。</summary>
    /// <param name="window">ウィンドウ。</param>
    /// <param name="message">メッセージ。</param>
    /// <param name="wParam">1 つ目の値。</param>
    /// <param name="lParam">2 つ目の値。</param>
    /// <returns>処理の結果。</returns>
    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
    public static partial nint DefWindowProc(nint window, uint message, nint wParam, nint lParam);

    /// <summary>次のメッセージを待って取り出す（GetMessageW）。</summary>
    /// <param name="message">取り出したメッセージ。</param>
    /// <param name="window">対象のウィンドウ。0 ならすべて。</param>
    /// <param name="filterMin">番号の下限。</param>
    /// <param name="filterMax">番号の上限。</param>
    /// <returns>終わりのメッセージなら 0、失敗なら -1、それ以外は 0 でない値。</returns>
    [LibraryImport("user32.dll", EntryPoint = "GetMessageW")]
    public static partial int GetMessage(Msg* message, nint window, uint filterMin, uint filterMax);

    /// <summary>キー入力のメッセージを文字のメッセージに変える（TranslateMessage）。</summary>
    /// <param name="message">メッセージ。</param>
    /// <returns>変えたら true。</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TranslateMessage(Msg* message);

    /// <summary>メッセージをウィンドウに届ける（DispatchMessageW）。</summary>
    /// <param name="message">メッセージ。</param>
    /// <returns>処理の結果。</returns>
    [LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")]
    public static partial nint DispatchMessage(Msg* message);

    /// <summary>メッセージの繰り返しを終わらせる（PostQuitMessage）。</summary>
    /// <param name="exitCode">終了コード。</param>
    [LibraryImport("user32.dll")]
    public static partial void PostQuitMessage(int exitCode);

    /// <summary>メッセージを送って待たずに戻る（PostMessageW）。別のスレッドからでも呼べる。</summary>
    /// <param name="window">ウィンドウ。</param>
    /// <param name="message">メッセージ。</param>
    /// <param name="wParam">1 つ目の値。</param>
    /// <param name="lParam">2 つ目の値。</param>
    /// <returns>成功したら true。</returns>
    [LibraryImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostMessage(nint window, uint message, nint wParam, nint lParam);

    /// <summary>名前つきのメッセージの番号を得る（RegisterWindowMessageW）。</summary>
    /// <param name="name">メッセージの名前。</param>
    /// <returns>メッセージの番号。失敗したら 0。</returns>
    [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint RegisterWindowMessage(string name);

    /// <summary>空のポップアップ メニューを作る（CreatePopupMenu）。</summary>
    /// <returns>メニューのハンドル。</returns>
    [LibraryImport("user32.dll")]
    public static partial nint CreatePopupMenu();

    /// <summary>メニューに項目を足す（AppendMenuW）。</summary>
    /// <param name="menu">メニュー。</param>
    /// <param name="flags">項目の形式（MF_*）。</param>
    /// <param name="id">項目の番号。</param>
    /// <param name="text">項目の文字。区切り線なら null。</param>
    /// <returns>成功したら true。</returns>
    [LibraryImport("user32.dll", EntryPoint = "AppendMenuW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AppendMenu(nint menu, uint flags, nuint id, string? text);

    /// <summary>メニューを壊す（DestroyMenu）。</summary>
    /// <param name="menu">メニュー。</param>
    /// <returns>成功したら true。</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyMenu(nint menu);

    /// <summary>ポップアップ メニューを出して、選ばれるまで待つ（TrackPopupMenuEx）。</summary>
    /// <param name="menu">メニュー。</param>
    /// <param name="flags">出し方（TPM_*）。</param>
    /// <param name="x">横の位置。</param>
    /// <param name="y">縦の位置。</param>
    /// <param name="window">持ち主のウィンドウ。</param>
    /// <param name="parameters">追加の指定。使わないなら 0。</param>
    /// <returns>TPM_RETURNCMD のとき、選ばれた項目の番号。選ばれなければ 0。</returns>
    [LibraryImport("user32.dll")]
    public static partial int TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint window, nint parameters);

    /// <summary>ウィンドウを前面にする（SetForegroundWindow）。メニューを外側のクリックで閉じられるようにするため。</summary>
    /// <param name="window">ウィンドウ。</param>
    /// <returns>成功したら true。</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(nint window);

    /// <summary>画像を読み込む（LoadImageW）。</summary>
    /// <param name="instance">モジュールのハンドル。ファイルから読むなら 0。</param>
    /// <param name="name">ファイルのパス。</param>
    /// <param name="type">画像の種類。</param>
    /// <param name="width">幅。</param>
    /// <param name="height">高さ。</param>
    /// <param name="flags">読み込み方。</param>
    /// <returns>画像のハンドル。失敗したら 0。</returns>
    [LibraryImport("user32.dll", EntryPoint = "LoadImageW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint LoadImage(nint instance, string name, uint type, int width, int height, uint flags);

    /// <summary>アイコンを壊す（DestroyIcon）。</summary>
    /// <param name="icon">アイコン。</param>
    /// <returns>成功したら true。</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyIcon(nint icon);

    /// <summary>画面の大きさなどを取得する（GetSystemMetrics）。</summary>
    /// <param name="index">取得する項目。</param>
    /// <returns>値。</returns>
    [LibraryImport("user32.dll")]
    public static partial int GetSystemMetrics(int index);

    /// <summary>トレイのアイコンを足す・変える・消す（Shell_NotifyIconW）。</summary>
    /// <param name="message">何をするか（NIM_*）。</param>
    /// <param name="data">アイコンの内容。</param>
    /// <returns>成功したら true。</returns>
    [LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShellNotifyIcon(uint message, NotifyIconData* data);

    /// <summary>利用者が通知を受けられる状態かを調べる（SHQueryUserNotificationState）。</summary>
    /// <param name="state">状態（QUNS_*）。</param>
    /// <returns>HRESULT。成功したら 0。</returns>
    [LibraryImport("shell32.dll")]
    public static partial int SHQueryUserNotificationState(int* state);

    /// <summary>
    /// 文字列を、固定長の文字の欄にコピーする。入りきらない分は切り捨て、必ず終端の文字を入れる。
    /// </summary>
    /// <param name="text">コピーする文字列。</param>
    /// <param name="destination">コピー先の欄の先頭。</param>
    /// <param name="capacity">欄の長さ（終端を含む文字数）。</param>
    public static void CopyToBuffer(string text, char* destination, int capacity)
    {
        var length = Math.Min(text.Length, capacity - 1);
        text.AsSpan(0, length).CopyTo(new Span<char>(destination, capacity));
        destination[length] = '\0';
    }
}

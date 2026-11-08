namespace Yamori.Notifier.Windowing;

/// <summary>
/// 隠しウィンドウに届いたメッセージ。
/// </summary>
/// <param name="message">メッセージの番号。</param>
/// <param name="wParam">1 つ目の値。</param>
/// <param name="lParam">2 つ目の値。</param>
internal sealed class WindowMessageEventArgs(uint message, nint wParam, nint lParam) : EventArgs
{
    /// <summary>メッセージの番号を取得する。</summary>
    public uint Message { get; } = message;

    /// <summary>1 つ目の値を取得する。</summary>
    public nint WParam { get; } = wParam;

    /// <summary>2 つ目の値を取得する。</summary>
    public nint LParam { get; } = lParam;

    /// <summary>処理したかどうかを取得・設定する。true にすると既定の処理をしない。</summary>
    public bool Handled { get; set; }
}

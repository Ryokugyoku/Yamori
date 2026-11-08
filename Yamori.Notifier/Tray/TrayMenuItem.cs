namespace Yamori.Notifier.Tray;

/// <summary>
/// トレイのメニューの 1 項目。
/// </summary>
/// <param name="Command">選んだときの操作。区切り線なら null。</param>
/// <param name="Text">項目の文字。区切り線なら空。</param>
/// <param name="IsEnabled">押せるなら true。</param>
internal sealed record TrayMenuItem(TrayCommand? Command, string Text, bool IsEnabled = true)
{
    /// <summary>区切り線。</summary>
    public static TrayMenuItem Separator { get; } = new(null, string.Empty);
}

using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;

namespace Yamori.Services;

/// <summary>
/// Windows の「アプリ モード」（ライト／ダーク）を調べ、変わったら知らせる。
/// </summary>
public sealed class SystemThemeWatcher : IDisposable
{
    private readonly UISettings _settings = new();
    private readonly DispatcherQueue _dispatcherQueue;

    /// <summary>
    /// 通知を UI スレッドで受け取るために、UI スレッドから作る。
    /// </summary>
    /// <exception cref="InvalidOperationException">UI スレッド以外から呼ばれた。</exception>
    public SystemThemeWatcher()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("SystemThemeWatcher は UI スレッドで作成してください。");
        _settings.ColorValuesChanged += OnColorValuesChanged;
    }

    /// <summary>
    /// Windows のアプリ モードが変わったときに、UI スレッドで発生する。
    /// </summary>
    public event EventHandler<ElementTheme>? ThemeChanged;

    /// <summary>
    /// 今の Windows のアプリ モードを取得する。
    /// </summary>
    public ElementTheme CurrentTheme => IsDark(_settings) ? ElementTheme.Dark : ElementTheme.Light;

    /// <summary>
    /// 変更の監視をやめる。
    /// </summary>
    public void Dispose() => _settings.ColorValuesChanged -= OnColorValuesChanged;

    private void OnColorValuesChanged(UISettings sender, object args) =>
        _dispatcherQueue.TryEnqueue(() => ThemeChanged?.Invoke(this, CurrentTheme));

    /// <summary>
    /// アプリの背景色が暗いかどうかで、ダーク モードかを判定する（Microsoft が案内している方法）。
    /// </summary>
    private static bool IsDark(UISettings settings)
    {
        var background = settings.GetColorValue(UIColorType.Background);
        return (5 * background.G) + (2 * background.R) + background.B < 8 * 128;
    }
}

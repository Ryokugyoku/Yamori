using Yamori.Notifier.Interop;
using Yamori.Notifier.Windowing;

namespace Yamori.Notifier.Tray;

/// <summary>
/// トレイのアイコンの右クリックで出すメニュー。出して、選ばれた項目を返す。
/// </summary>
internal static class TrayMenu
{
    /// <summary>
    /// メニューを出し、選ばれるか閉じられるまで待つ。UI スレッドから呼ぶこと。
    /// </summary>
    /// <param name="window">メニューの持ち主の隠しウィンドウ。</param>
    /// <param name="position">メニューを出す画面上の位置。</param>
    /// <param name="items">メニューの項目。</param>
    /// <returns>選ばれた操作。何も選ばずに閉じたら null。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="window"/> か <paramref name="items"/> が null。</exception>
    public static TrayCommand? Show(MessageWindow window, NativeMethods.Point position, IReadOnlyList<TrayMenuItem> items)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(items);

        var menu = NativeMethods.CreatePopupMenu();
        if (menu == 0)
        {
            return null;
        }

        try
        {
            foreach (var item in items)
            {
                if (item.Command is not { } command)
                {
                    NativeMethods.AppendMenu(menu, NativeMethods.MfSeparator, 0, null);
                    continue;
                }

                var flags = NativeMethods.MfString | (item.IsEnabled ? 0 : NativeMethods.MfGrayed);
                NativeMethods.AppendMenu(menu, flags, (nuint)(int)command, item.Text);
            }

            // 前面にしておかないと、メニューの外側をクリックしても閉じない（Windows の決まりごと）。
            NativeMethods.SetForegroundWindow(window.Handle);
            var selected = NativeMethods.TrackPopupMenuEx(
                menu,
                NativeMethods.TpmReturnCmd | NativeMethods.TpmRightButton,
                position.X,
                position.Y,
                window.Handle,
                0);
            NativeMethods.PostMessage(window.Handle, NativeMethods.WmNull, 0, 0);

            return Enum.IsDefined(typeof(TrayCommand), selected) ? (TrayCommand)selected : null;
        }
        finally
        {
            NativeMethods.DestroyMenu(menu);
        }
    }
}

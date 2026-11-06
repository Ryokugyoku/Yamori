using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.Windows.Storage.Pickers;

namespace Yamori.Services;

/// <summary>
/// Windows App SDK のフォルダー選択画面を使って、フォルダーを選んでもらう。
/// </summary>
public sealed class WindowsFolderPicker : IFolderPicker
{
    private readonly Func<WindowId> _getWindowId;

    /// <summary>
    /// 選択画面の親にするウィンドウを指定して作る。
    /// </summary>
    /// <param name="getWindowId">親ウィンドウの ID を返す処理。</param>
    /// <exception cref="ArgumentNullException"><paramref name="getWindowId"/> が null。</exception>
    public WindowsFolderPicker(Func<WindowId> getWindowId)
    {
        ArgumentNullException.ThrowIfNull(getWindowId);
        _getWindowId = getWindowId;
    }

    /// <inheritdoc />
    public async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker(_getWindowId())
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };

        var result = await picker.PickSingleFolderAsync();
        return result?.Path;
    }
}

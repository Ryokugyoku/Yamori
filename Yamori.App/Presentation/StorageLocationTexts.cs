using System;
using Yamori.Core.Settings;
using Yamori.Core.Storage;
using Yamori.Localization;

namespace Yamori.Presentation;

/// <summary>
/// 記録の保存先について、画面に出す説明文を作る。
/// </summary>
public static class StorageLocationTexts
{
    /// <summary>
    /// 保存先に応じた、通信とデータの扱いについての説明を作る。
    /// </summary>
    /// <param name="location">今の保存先。</param>
    /// <returns>説明文。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="location"/> が null。</exception>
    public static string PrivacyNote(HistoryLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        return location.Kind switch
        {
            StorageLocationKind.OneDrive => UiText.Get("Privacy_OneDrive"),
            StorageLocationKind.CustomFolder => UiText.Get("Privacy_Custom"),
            _ => UiText.Get("Privacy_Local"),
        };
    }

    /// <summary>
    /// 保存先のフォルダーを示す文字列を作る。
    /// </summary>
    /// <param name="location">今の保存先。</param>
    /// <returns>例：「保存先：C:\...\history」。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="location"/> が null。</exception>
    public static string PathLabel(HistoryLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        return UiText.Format("Storage_PathLabel", location.Directory);
    }

    /// <summary>
    /// 選んだ保存先が使えず、この PC の中に保存しているときの説明を作る。
    /// </summary>
    /// <param name="location">今の保存先。</param>
    /// <returns>説明文。代わりの保存先を使っていなければ空。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="location"/> が null。</exception>
    public static string FallbackNote(HistoryLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        return location.IsFallback ? UiText.Get("Storage_Fallback") : string.Empty;
    }
}

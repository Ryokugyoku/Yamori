using System.Collections.Generic;
using System.Linq;
using Yamori.Core.Formatting;
using Yamori.Core.Hardware;
using Yamori.Localization;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// 各部品の表示内容を作るときに共通で使う処理。
/// </summary>
public static class HardwareSections
{
    /// <summary>
    /// 部品の種類のアイコンと名前を付けて、表示内容を作る。項目が 1 つもない場合は「取得できなかった」の 1 件を入れる。
    /// </summary>
    /// <param name="component">部品の種類。</param>
    /// <param name="entries">部品ごとの表示内容。</param>
    /// <param name="noteKey">説明のリソースのキー。説明がない場合は null。</param>
    /// <returns>部品の種類 1 つ分の表示内容。</returns>
    public static HardwareSection Create(HardwareComponent component, IReadOnlyList<HardwareEntry> entries, string? noteKey = null) =>
        new(
            HardwareGlyphs.For(component),
            ComponentDisplayNames.Get(component),
            entries.Count > 0 ? entries : [NotAvailable()],
            noteKey is null ? string.Empty : UiText.Get(noteKey));

    /// <summary>
    /// 「情報を取得できなかった」ことを表す 1 件を作る。
    /// </summary>
    /// <returns>表示内容 1 件。</returns>
    public static HardwareEntry NotAvailable() =>
        new(UiText.Get("Hw_NotAvailable_Title"), UiText.Get("Hw_NotAvailable_Detail"), []);

    /// <summary>
    /// 空でない値だけを「・」などの区切りでつなぐ。
    /// </summary>
    /// <param name="parts">つなぐ値。null や空の値は除く。</param>
    /// <returns>つないだ文字列。</returns>
    public static string Join(params string?[] parts) =>
        string.Join(UiText.Get("Hw_Separator"), parts.Where(p => !string.IsNullOrWhiteSpace(p)));
}

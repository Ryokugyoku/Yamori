using System.Collections.Generic;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// ハードウェア画面の、部品の種類 1 つ分（例：CPU）の表示内容。
/// </summary>
/// <param name="Glyph">部品の種類を表すアイコン（Segoe Fluent Icons の文字）。</param>
/// <param name="Category">部品の種類の名前（例：CPU）。</param>
/// <param name="Entries">この種類に属する部品（例：GPU が 2 枚ならその 2 件）。</param>
/// <param name="Note">数値の意味や、取得できない情報についての説明。ない場合は空文字列。</param>
public sealed record HardwareSection(string Glyph, string Category, IReadOnlyList<HardwareEntry> Entries, string Note)
{
    /// <summary>説明があるかどうか。x:Bind で表示・非表示を切り替えるのに使う。</summary>
    public bool HasNote => Note.Length > 0;
}

namespace Yamori.Presentation;

/// <summary>
/// 概要画面の一番上に出す、この PC の主な仕様 1 つ分（例：メモリ 32 GB）。
/// </summary>
/// <param name="Glyph">部品の種類を表すアイコン（Segoe Fluent Icons の文字）。</param>
/// <param name="Label">部品の種類の名前（例：メモリ）。</param>
/// <param name="Value">主な値（例：32 GB、製品名）。</param>
/// <param name="Detail">補足（例：DDR5・2 枚）。ない場合は空文字列。</param>
public sealed record SpecItem(string Glyph, string Label, string Value, string Detail)
{
    /// <summary>補足があるかどうか。x:Bind で表示・非表示を切り替えるのに使う。</summary>
    public bool HasDetail => Detail.Length > 0;
}

using System.Collections.Generic;

namespace Yamori.Presentation.Realtime;

/// <summary>
/// リアルタイム画面の、部品の種類 1 つ分（例：CPU）の、ある時点の表示内容。
/// </summary>
/// <param name="Key">更新のたびに同じ種類を見分けるためのキー。</param>
/// <param name="Glyph">部品の種類を表すアイコン（Segoe Fluent Icons の文字）。</param>
/// <param name="Category">部品の種類の名前。</param>
/// <param name="Headline">見出しに大きく出す、いちばん大事な値。</param>
/// <param name="HeadlinePercent">見出しの下の棒グラフで示す割合（0〜100）。棒グラフを出さない場合は null。</param>
/// <param name="Note">数値の意味や、測れない値についての説明。</param>
/// <param name="Entries">この種類に属する部品。</param>
public sealed record RealtimeSectionData(
    string Key,
    string Glyph,
    string Category,
    string Headline,
    double? HeadlinePercent,
    string Note,
    IReadOnlyList<RealtimeEntryData> Entries);

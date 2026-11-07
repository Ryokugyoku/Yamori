using System.Collections.Generic;

namespace Yamori.Presentation.Hardware;

/// <summary>
/// ハードウェア画面の、部品 1 つ分（例：メモリモジュール 1 枚）の表示内容。
/// </summary>
/// <param name="Title">製品名など、いちばん目立たせる内容。</param>
/// <param name="Subtitle">容量や種類などの短い補足。ない場合は空文字列。</param>
/// <param name="Facts">項目名と値の組の一覧。</param>
public sealed record HardwareEntry(string Title, string Subtitle, IReadOnlyList<HardwareFact> Facts)
{
    /// <summary>補足があるかどうか。x:Bind で表示・非表示を切り替えるのに使う。</summary>
    public bool HasSubtitle => Subtitle.Length > 0;
}

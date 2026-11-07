using System.Collections.Generic;

namespace Yamori.Presentation.Realtime;

/// <summary>
/// リアルタイム画面の、部品 1 つ（GPU 1 枚、ディスク 1 台など）の、ある時点の表示内容。
/// </summary>
/// <param name="Key">更新のたびに同じ部品を見分けるためのキー。</param>
/// <param name="Title">部品の名前。</param>
/// <param name="Subtitle">名前の下に添える説明。ない場合は空文字列。</param>
/// <param name="Facts">項目の一覧。</param>
public sealed record RealtimeEntryData(string Key, string Title, string Subtitle, IReadOnlyList<RealtimeFactData> Facts);

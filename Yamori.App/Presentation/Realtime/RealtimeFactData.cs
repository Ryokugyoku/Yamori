namespace Yamori.Presentation.Realtime;

/// <summary>
/// リアルタイム画面の項目 1 つ（項目名と値）の、ある時点の表示内容。
/// </summary>
/// <param name="Key">更新のたびに同じ項目を見分けるためのキー。</param>
/// <param name="Label">項目名。</param>
/// <param name="Value">値の表示。</param>
/// <param name="Percent">棒グラフで示す割合（0〜100）。棒グラフを出さない項目は null。</param>
public sealed record RealtimeFactData(string Key, string Label, string Value, double? Percent);

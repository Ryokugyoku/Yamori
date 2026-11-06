namespace Yamori.Presentation;

/// <summary>
/// ハードウェア一覧の 1 件分の表示内容。
/// </summary>
/// <param name="Glyph">部品の種類を表すアイコン（Segoe Fluent Icons の文字）。</param>
/// <param name="Category">部品の種類（例：CPU）。</param>
/// <param name="Title">製品名など、いちばん目立たせる内容。</param>
/// <param name="Detail">容量や状態などの補足と、その意味。</param>
public sealed record HardwareRow(string Glyph, string Category, string Title, string Detail);

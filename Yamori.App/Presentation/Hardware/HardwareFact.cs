namespace Yamori.Presentation.Hardware;

/// <summary>
/// 部品の情報の 1 項目（例：「ソケット」「LGA1700」）。
/// </summary>
/// <param name="Label">項目名。</param>
/// <param name="Value">値と、必要ならその意味。</param>
public sealed record HardwareFact(string Label, string Value);

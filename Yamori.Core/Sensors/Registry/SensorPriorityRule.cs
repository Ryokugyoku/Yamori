namespace Yamori.Core.Sensors.Registry;

/// <summary>
/// パターンに一致するキーについて、Provider の優先順を決める規則。
/// </summary>
/// <param name="Pattern">対象のキーのパターン（<see cref="SensorKeyPattern"/> の表記）。</param>
/// <param name="Order">優先する Provider の ID を、優先する順に並べたもの。ここにない Provider は既定の順で後ろに続く。</param>
public sealed record SensorPriorityRule(string Pattern, IReadOnlyList<string> Order);

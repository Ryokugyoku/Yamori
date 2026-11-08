namespace Yamori.Core.Sensors.Display;

/// <summary>
/// リアルタイム画面の詳細で、1 枚のカードにまとめて見せるセンサーの集まり。
/// グループの中に量の種類が混ざっている場合（スレッドごとの使用率とクロックなど）は、種類ごとに分けて 1 枚ずつにする。
/// </summary>
/// <param name="Key">カードを識別するキー（グループのキー、分けた場合は <c>{groupKey}/{type}</c>）。</param>
/// <param name="Group">グループの名前（<see cref="SensorDescriptor.Group"/>）。</param>
/// <param name="SplitType">種類ごとに分けた場合は、その量の種類。分けていなければ null。</param>
/// <param name="Sensors">センサー。</param>
/// <param name="Labels">センサーごとの短い表示名（<paramref name="Sensors"/> と同じ順）。全員に共通する末尾は取り除いてある。</param>
/// <param name="CommonSuffix">取り除いた共通の末尾（例：<c>Clock (estimated)</c>）。なければ空。</param>
/// <param name="IsDense">センサーの数が多く、小さなマスで詰めて並べるかどうか。</param>
public sealed record SensorPanel(
    string Key,
    string Group,
    SensorType? SplitType,
    IReadOnlyList<SensorDescriptor> Sensors,
    IReadOnlyList<string> Labels,
    string CommonSuffix,
    bool IsDense);

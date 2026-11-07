namespace Yamori.Core.Sensors.Registry;

/// <summary>
/// <see cref="SensorPriorityOptions"/> に従って、キーごとの Provider の優先順を決める。
/// </summary>
public sealed class SensorPriorityPolicy
{
    private readonly IReadOnlyList<string> _defaultOrder;
    private readonly IReadOnlyList<(SensorKeyPattern Pattern, IReadOnlyList<string> Order)> _rules;

    /// <summary>
    /// 設定を指定して作る。
    /// </summary>
    /// <param name="options">優先度の設定。</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> が null。</exception>
    /// <exception cref="FormatException">規則のパターンの表記が正しくない。</exception>
    public SensorPriorityPolicy(SensorPriorityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _defaultOrder = options.DefaultOrder ?? [];
        _rules = (options.Rules ?? [])
            .Select(r => (SensorKeyPattern.Parse(r.Pattern), r.Order ?? (IReadOnlyList<string>)[]))
            .ToArray();
    }

    /// <summary>
    /// キーを提供している Provider を、優先する順に並べる。
    /// </summary>
    /// <param name="key">センサーのキー。</param>
    /// <param name="providerIds">そのキーを提供している Provider の ID。</param>
    /// <returns>優先する順に並べた ID。最初に一致した規則の順、既定の順、設定にない ID の順（文字列の順）で並べる。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="providerIds"/> が null。</exception>
    public IReadOnlyList<string> Order(SensorKey key, IEnumerable<string> providerIds)
    {
        ArgumentNullException.ThrowIfNull(providerIds);

        var rule = _rules.FirstOrDefault(r => r.Pattern.Matches(key)).Order ?? [];
        return providerIds
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => Rank(rule, id))
            .ThenBy(id => Rank(_defaultOrder, id))
            .ThenBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    private static int Rank(IReadOnlyList<string> order, string id)
    {
        for (var i = 0; i < order.Count; i++)
        {
            if (string.Equals(order[i], id, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return int.MaxValue;
    }
}

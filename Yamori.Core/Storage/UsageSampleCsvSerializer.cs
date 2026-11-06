using System.Globalization;
using Yamori.Core.Hardware;
using Yamori.Core.Monitoring;

namespace Yamori.Core.Storage;

/// <summary>
/// <see cref="ComponentUsageSample"/> と CSV の 1 行を相互に変換する。
/// 形式は <c>時刻(ISO 8601, UTC),部品,使用率</c>。言語設定に左右されないよう InvariantCulture を使う。
/// </summary>
public sealed class UsageSampleCsvSerializer
{
    private const char Separator = ',';

    /// <summary>
    /// サンプルを CSV の 1 行に変換する。
    /// </summary>
    /// <param name="sample">変換するサンプル。</param>
    /// <returns>改行を含まない CSV の 1 行。</returns>
    public string Serialize(ComponentUsageSample sample) =>
        string.Join(
            Separator,
            sample.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            sample.Component.ToString(),
            sample.UsagePercent.ToString("0.##", CultureInfo.InvariantCulture));

    /// <summary>
    /// CSV の 1 行をサンプルに変換する。
    /// </summary>
    /// <param name="line">CSV の 1 行。</param>
    /// <param name="sample">変換できた場合のサンプル。</param>
    /// <returns>変換できた場合は true。壊れた行は false。</returns>
    public bool TryDeserialize(string? line, out ComponentUsageSample sample)
    {
        sample = default;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var parts = line.Split(Separator);
        if (parts.Length != 3
            || !DateTimeOffset.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp)
            || !Enum.TryParse<HardwareComponent>(parts[1], ignoreCase: false, out var component)
            || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var usage))
        {
            return false;
        }

        sample = new ComponentUsageSample(component, timestamp, usage);
        return true;
    }
}

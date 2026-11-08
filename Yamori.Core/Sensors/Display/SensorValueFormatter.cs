using System.Globalization;
using Yamori.Core.Formatting;

namespace Yamori.Core.Sensors.Display;

/// <summary>
/// 基準単位の値を、読みやすい単位つきの文字列にする（例：4.5e9 Hz → <c>4.50 GHz</c>）。
/// </summary>
public static class SensorValueFormatter
{
    /// <summary>値がないときの表示。</summary>
    public const string Unavailable = "—";

    /// <summary>オン・オフの印がオンのときの表示。</summary>
    public const string FlagOn = "On";

    /// <summary>オン・オフの印がオフのときの表示。</summary>
    public const string FlagOff = "Off";

    /// <summary>
    /// 値を単位つきの文字列にする。
    /// </summary>
    /// <param name="value">値（基準単位）。null なら <see cref="Unavailable"/>。</param>
    /// <param name="unit">単位。</param>
    /// <param name="culture">数値の書式に使うカルチャ。</param>
    /// <returns>単位つきの文字列。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="culture"/> が null。</exception>
    public static string Format(double? value, SensorUnit unit, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        if (value is not { } v || !double.IsFinite(v))
        {
            return Unavailable;
        }

        return unit switch
        {
            SensorUnit.Percent => v.ToString("0.#", culture) + "%",
            SensorUnit.Hertz => FormatHertz(v, culture),
            SensorUnit.Celsius => v.ToString("0.#", culture) + " °C",
            SensorUnit.Watt => v.ToString(v >= 100 ? "0" : "0.#", culture) + " W",
            SensorUnit.Volt => v.ToString("0.000", culture) + " V",
            SensorUnit.Ampere => v.ToString("0.##", culture) + " A",
            SensorUnit.Rpm => v.ToString("N0", culture) + " RPM",
            SensorUnit.Byte => ByteSizeFormatter.Format(ToBytes(v)),
            SensorUnit.BytePerSecond => ByteSizeFormatter.Format(ToBytes(v)) + "/s",
            SensorUnit.PerSecond => v.ToString("N0", culture) + " /s",
            SensorUnit.Count => v.ToString("N0", culture),
            SensorUnit.Second => FormatSeconds(v, culture),
            SensorUnit.WattHour => v.ToString("0.#", culture) + " Wh",
            SensorUnit.Flag => v != 0 ? FlagOn : FlagOff,
            _ => v.ToString("G4", culture),
        };
    }

    /// <summary>
    /// 値をバーで表示できる割合（0〜100）にする。
    /// </summary>
    /// <param name="value">値（基準単位）。</param>
    /// <param name="unit">単位。</param>
    /// <returns>単位が % なら 0〜100 に収めた値。それ以外か、値がなければ null。</returns>
    public static double? ToPercent(double? value, SensorUnit unit) =>
        unit == SensorUnit.Percent && value is { } v && double.IsFinite(v) ? Math.Clamp(v, 0, 100) : null;

    private static string FormatHertz(double hertz, CultureInfo culture) =>
        hertz >= 1e9 ? (hertz / 1e9).ToString("0.00", culture) + " GHz"
        : hertz >= 1e6 ? (hertz / 1e6).ToString("0", culture) + " MHz"
        : hertz.ToString("0", culture) + " Hz";

    private static string FormatSeconds(double seconds, CultureInfo culture)
    {
        if (seconds < 1)
        {
            return (seconds * 1000).ToString("0.##", culture) + " ms";
        }

        if (seconds < 60)
        {
            return seconds.ToString("0.#", culture) + " s";
        }

        var span = TimeSpan.FromSeconds(seconds);
        return span.TotalDays >= 1
            ? string.Create(culture, $"{(int)span.TotalDays}d {span.Hours:00}:{span.Minutes:00}:{span.Seconds:00}")
            : string.Create(culture, $"{span.Hours:00}:{span.Minutes:00}:{span.Seconds:00}");
    }

    private static ulong ToBytes(double value) => value <= 0 ? 0 : value >= ulong.MaxValue ? ulong.MaxValue : (ulong)value;
}

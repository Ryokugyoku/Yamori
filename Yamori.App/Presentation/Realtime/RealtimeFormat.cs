using Yamori.Core.Formatting;
using Yamori.Localization;

namespace Yamori.Presentation.Realtime;

/// <summary>
/// リアルタイム画面のアプリごとの使用量を、単位つきの文字列にする。値が null の場合は null を返す（項目ごと表示しないため）。
/// </summary>
/// <remarks>センサーの値の表示は <see cref="Yamori.Core.Sensors.Display.SensorValueFormatter"/> で行う。</remarks>
public static class RealtimeFormat
{
    /// <summary>
    /// 割合を「12.3%」の形にする。
    /// </summary>
    /// <param name="percent">割合（%）。</param>
    /// <returns>表示用の文字列。</returns>
    public static string? Percent(double? percent) =>
        percent is { } value ? UiText.Format("Rt_Value_Percent", value) : null;

    /// <summary>
    /// 1 秒あたりのバイト数を単位つきにする（1024 進）。
    /// </summary>
    /// <param name="bytesPerSecond">1 秒あたりのバイト数。</param>
    /// <returns>表示用の文字列。</returns>
    public static string? BytesPerSecond(double? bytesPerSecond) =>
        bytesPerSecond is >= 0 and var value
            ? UiText.Format("Rt_Value_BytesPerSecond", ByteSizeFormatter.Format((ulong)value))
            : null;
}

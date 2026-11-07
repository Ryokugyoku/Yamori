using System.Globalization;
using Yamori.Core.Sensors;
using Yamori.Core.Sensors.Display;

namespace Yamori.ViewModels.Sensors;

/// <summary>
/// 最新値と表示の状態から、値の表示（文字列・棒グラフ・不透明度）を決める。
/// </summary>
public static class SensorRowFiller
{
    /// <summary>一時的に読めないときの不透明度。</summary>
    public const double UnavailableOpacity = 0.45;

    /// <summary>
    /// 値の表示を決める。
    /// </summary>
    /// <param name="descriptor">センサーの説明。</param>
    /// <param name="reading">最新値。まだなければ null。</param>
    /// <param name="state">表示の状態。</param>
    /// <returns>値の文字列、棒グラフの割合（なければ null）、不透明度。</returns>
    public static (string Text, double? Percent, double Opacity) Describe(SensorDescriptor descriptor, SensorReading? reading, SensorDisplayState state)
    {
        var value = state == SensorDisplayState.Value ? reading?.Value : null;
        return (
            SensorValueFormatter.Format(value, descriptor.Unit, CultureInfo.CurrentUICulture),
            SensorValueFormatter.ToPercent(value, descriptor.Unit),
            value is null ? UnavailableOpacity : 1);
    }
}

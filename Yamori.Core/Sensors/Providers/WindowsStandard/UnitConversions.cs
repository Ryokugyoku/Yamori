namespace Yamori.Core.Sensors.Providers.WindowsStandard;

/// <summary>
/// 標準 API が返す単位から、センサーの基準単位への変換。null はそのまま null を返す。
/// </summary>
public static class UnitConversions
{
    /// <summary>MHz を Hz に変換する。</summary>
    /// <param name="megahertz">MHz の値。</param>
    /// <returns>Hz の値。</returns>
    public static double? MegahertzToHertz(double? megahertz) => megahertz * 1_000_000;

    /// <summary>mV を V に変換する。</summary>
    /// <param name="millivolts">mV の値。</param>
    /// <returns>V の値。</returns>
    public static double? MillivoltsToVolts(double? millivolts) => millivolts / 1_000;

    /// <summary>mW を W に変換する。</summary>
    /// <param name="milliwatts">mW の値。</param>
    /// <returns>W の値。</returns>
    public static double? MilliwattsToWatts(double? milliwatts) => milliwatts / 1_000;

    /// <summary>mWh を Wh に変換する。</summary>
    /// <param name="milliwattHours">mWh の値。</param>
    /// <returns>Wh の値。</returns>
    public static double? MilliwattHoursToWattHours(double? milliwattHours) => milliwattHours / 1_000;

    /// <summary>ミリ秒を秒に変換する。</summary>
    /// <param name="milliseconds">ミリ秒の値。</param>
    /// <returns>秒の値。</returns>
    public static double? MillisecondsToSeconds(double? milliseconds) => milliseconds / 1_000;

    /// <summary>ビット毎秒をバイト毎秒に変換する。</summary>
    /// <param name="bitsPerSecond">ビット毎秒の値。</param>
    /// <returns>バイト毎秒の値。</returns>
    public static double? BitsToBytesPerSecond(double? bitsPerSecond) => bitsPerSecond / 8;
}

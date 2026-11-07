namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 解像度ごとの区間の長さと、Column Family の名前。
/// </summary>
public static class TimeSeriesResolutions
{
    /// <summary>1 分（ミリ秒）。</summary>
    public const long OneMinuteMs = 60_000;

    /// <summary>1 時間（ミリ秒）。</summary>
    public const long OneHourMs = 3_600_000;

    /// <summary>集計の解像度（集計する順）。</summary>
    public static IReadOnlyList<TimeSeriesResolution> Rollups { get; } = [TimeSeriesResolution.OneMinute, TimeSeriesResolution.OneHour];

    /// <summary>
    /// 集計の区間の長さを取得する。
    /// </summary>
    /// <param name="resolution">集計の解像度。</param>
    /// <returns>区間の長さ（ミリ秒）。</returns>
    /// <exception cref="ArgumentOutOfRangeException">生データか、定義されていない値。</exception>
    public static long BucketMs(TimeSeriesResolution resolution) => resolution switch
    {
        TimeSeriesResolution.OneMinute => OneMinuteMs,
        TimeSeriesResolution.OneHour => OneHourMs,
        _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "集計の解像度ではありません。"),
    };

    /// <summary>
    /// Column Family の名前を取得する。
    /// </summary>
    /// <param name="resolution">解像度。</param>
    /// <returns><c>raw</c>、<c>rollup_1m</c>、<c>rollup_1h</c> のいずれか。</returns>
    /// <exception cref="ArgumentOutOfRangeException">定義されていない値。</exception>
    public static string ColumnFamilyName(TimeSeriesResolution resolution) => resolution switch
    {
        TimeSeriesResolution.Raw => "raw",
        TimeSeriesResolution.OneMinute => "rollup_1m",
        TimeSeriesResolution.OneHour => "rollup_1h",
        _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "定義されていない解像度です。"),
    };

    /// <summary>
    /// 時刻を、その区間の始まりに切り下げる。
    /// </summary>
    /// <param name="timestampMs">時刻（UTC の Unix ミリ秒）。</param>
    /// <param name="bucketMs">区間の長さ（ミリ秒）。</param>
    /// <returns>区間の始まり。</returns>
    public static long FloorToBucket(long timestampMs, long bucketMs) => timestampMs - (timestampMs % bucketMs);
}

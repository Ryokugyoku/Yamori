namespace Yamori.Core.Sensors.TimeSeries;

/// <summary>
/// 解像度ごとの保持期間。
/// </summary>
/// <param name="Raw">生データの保持期間。</param>
/// <param name="OneMinute">1 分集計の保持期間。</param>
/// <param name="OneHour">1 時間集計の保持期間。null なら無期限。</param>
public sealed record RetentionPolicy(TimeSpan Raw, TimeSpan OneMinute, TimeSpan? OneHour)
{
    /// <summary>初期値（生データ 7 日、1 分集計 90 日、1 時間集計は無期限）を取得する。</summary>
    public static RetentionPolicy Default { get; } = new(TimeSpan.FromDays(7), TimeSpan.FromDays(90), null);

    /// <summary>
    /// 解像度の保持期間を取得する。
    /// </summary>
    /// <param name="resolution">解像度。</param>
    /// <returns>保持期間。無期限なら null。</returns>
    /// <exception cref="ArgumentOutOfRangeException">定義されていない値。</exception>
    public TimeSpan? For(TimeSeriesResolution resolution) => resolution switch
    {
        TimeSeriesResolution.Raw => Raw,
        TimeSeriesResolution.OneMinute => OneMinute,
        TimeSeriesResolution.OneHour => OneHour,
        _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "定義されていない解像度です。"),
    };
}
